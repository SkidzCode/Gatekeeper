using MySqlConnector;
using System.Data;
using System.Web;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Services
{
    public interface IVerifyTokenService
    {
        public Task<(bool, User?, string)> VerifyTokenAsync(string verificationCode);
        public Task<string> GenerateTokenAsync(int userId, string verifyType);
        public Task<int> RevokeTokensAsync(int userId, string verifyType, string? token = null);
    }


    /// <summary>
    /// Service handling user authentication and related operations.
    /// </summary>
    public class VerifyTokenService : IVerifyTokenService
    {
        private readonly IDBHelper _dbHelper;
        private readonly ILogger<VerifyTokenService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        /// <summary>
        /// Constructor for VerificationService.
        /// </summary>
        /// <param name="configuration">Application configuration dependency.</param>
        /// <param name="dbHelper">Database helper for DB operations.</param>
        /// <param name="logger">Logger for logging information and errors.</param>
        public VerifyTokenService(IConfiguration configuration, IDBHelper dbHelper, ILogger<VerifyTokenService> logger, IEmailService emailService)
        {
            _configuration = configuration;
            _dbHelper = dbHelper;
            _logger = logger;
            _emailService = emailService;
        }

        /// <inheritdoc />
        public async Task<(bool, User?, string)> VerifyTokenAsync(string verificationCode)
        {
            var tokenId = verificationCode.Split('.')[0];
            User? user = null;
            string validationType = string.Empty;
            try
            {
                await using var connection = await _dbHelper.GetOpenConnectionAsync();
                await using var cmd = new MySqlCommand("ValidateUser", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@p_Id", tokenId);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync() || Convert.ToBoolean(reader["Revoked"]))
                    return (false, null, validationType);

                bool alreadyComplete = Convert.ToBoolean(reader["Complete"]);
                validationType = reader["VerifyType"].ToString() ?? string.Empty;
                string salt = reader["RefreshSalt"].ToString() ?? string.Empty;
                string storedHashedToken = reader["HashedToken"].ToString() ?? string.Empty;
                string providedTokenPart = verificationCode.Split('.')[1];
                var hashedProvidedToken = PasswordHelper.HashPassword(providedTokenPart, salt);

                if (storedHashedToken != hashedProvidedToken || alreadyComplete)
                    return (false, null, validationType);

                user = new User()
                {
                    Id = Convert.ToInt32(reader["UserId"]),
                    FirstName = reader["FirstName"].ToString() ?? string.Empty,
                    LastName = reader["LastName"].ToString() ?? string.Empty,
                    Email = reader["Email"].ToString() ?? string.Empty,
                    Phone = reader["Phone"].ToString() ?? string.Empty,
                    Salt = reader["Salt"].ToString() ?? string.Empty,
                    Password = reader["Password"].ToString() ?? string.Empty,
                    Username = reader["Username"].ToString() ?? string.Empty
                };
                reader?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token.");
                throw;
            }

            return (!string.IsNullOrEmpty(user.Username), user, validationType);
        }


        /// <summary>
        /// Generates and Stores the refresh token in the database.
        /// </summary>
        /// <param name="userId">User ID associated with the token.</param>
        /// <param name="refreshToken">Refresh token to store.</param>
        /// <param name="connection">Active database connection.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<string> GenerateTokenAsync(int userId, string verifyType)
        {
            var verifyToken = GenerateVerifyToken();
            MySqlConnection connection = await _dbHelper.GetOpenConnectionAsync();
            // Generate Refresh Token
            var salt = PasswordHelper.GenerateSalt();
            var hashedVerifyToken = PasswordHelper.HashPassword(verifyToken, salt);
            var tokenId = Guid.NewGuid().ToString(); // Unique identifier for the refresh token

            // Store Refresh Token in DB
            await using (var cmd = new MySqlCommand("VerificationInsert", connection))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_Id", tokenId);
                cmd.Parameters.AddWithValue("@p_VerifyType", verifyType);
                cmd.Parameters.AddWithValue("@p_UserId", userId);
                cmd.Parameters.AddWithValue("@p_HashedToken", hashedVerifyToken);
                cmd.Parameters.AddWithValue("@p_Salt", salt);
                cmd.Parameters.AddWithValue("@p_ExpiryDate", DateTime.UtcNow.AddDays(7)); // 7-day expiration
                // Add additional parameters as needed
                await cmd.ExecuteNonQueryAsync();
            }

            return $"{tokenId}.{verifyToken}";
        }

        /// <summary>
        /// Revokes tokens for a user, either specific or all tokens.
        /// </summary>
        /// <param name="token">Specific token to revoke, or null to revoke all tokens.</param>
        /// <param name="userId">ID of the user whose tokens are to be revoked.</param>
        /// <returns>The number of tokens revoked.</returns>
        public async Task<int> RevokeTokensAsync(int userId, string verifyType, string? token = null)
        {
            string? tokenId = null;
            if (!string.IsNullOrEmpty(token))
                tokenId = token.Split('.')[0];

            await using var connection = await _dbHelper.GetOpenConnectionAsync();

            await using var cmd = new MySqlCommand("RevokeVerifyToken", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new MySqlParameter("@p_UserId", MySqlDbType.Int32)).Value = userId;
            cmd.Parameters.Add(new MySqlParameter("@p_TokenId", MySqlDbType.VarChar, 36)).Value = tokenId ?? (object)DBNull.Value;
            cmd.Parameters.Add(new MySqlParameter("@p_VerifyType", MySqlDbType.VarChar, 20)).Value = verifyType;
            var rowsAffectedParam = new MySqlParameter("@p_RowsAffected", MySqlDbType.Int32)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(rowsAffectedParam);

            // Execute the procedure
            await cmd.ExecuteNonQueryAsync();

            // Get the value of the output parameter
            int rowsAffected = (int)rowsAffectedParam.Value;

            return rowsAffected;
        }


        #region Private Helper Methods


        /// <summary>
        /// Generates a secure refresh token.
        /// </summary>
        /// <returns>Refresh token as a string.</returns>
        private string GenerateVerifyToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }



        #endregion
    }


}

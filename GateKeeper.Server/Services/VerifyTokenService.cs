using MySqlConnector;
using System.Data;
using System.Web;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Services
{
    public interface IVerifyTokenService
    {
        public Task<(bool, User?, string)> VerifyTokenAsync(VerifyTokenRequest verificationCode);
        public Task<string> GenerateTokenAsync(int userId, string verifyType);
        public Task<int> RevokeTokensAsync(int userId, string verifyType, string? token = null);
    }


    /// <summary>
    /// Service handling user authentication and related operations.
    /// </summary>
    public class VerifyTokenService : IVerifyTokenService
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger<VerifyTokenService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        /// <summary>
        /// Constructor for VerificationService.
        /// </summary>
        /// <param name="configuration">Application configuration dependency.</param>
        /// <param name="dbHelper">Database helper for DB operations.</param>
        /// <param name="logger">Logger for logging information and errors.</param>
        public VerifyTokenService(IConfiguration configuration, IDbHelper dbHelper, ILogger<VerifyTokenService> logger, IEmailService emailService)
        {
            _configuration = configuration;
            _dbHelper = dbHelper;
            _logger = logger;
            _emailService = emailService;
        }

        /// <inheritdoc />
        public async Task<(bool, User?, string)> VerifyTokenAsync(VerifyTokenRequest verifyRequest)
        {
            string verificationCode = verifyRequest.VerificationCode;
            var tokenParts = verificationCode.Split('.');
            if (tokenParts.Length != 2)
            {
                _logger.LogWarning("Invalid verification code format.");
                return (false, null, string.Empty);
            }

            var tokenId = tokenParts[0];
            var providedTokenPart = tokenParts[1];
            string tokenUsername = string.Empty;
            User? user = null;
            string validationType = string.Empty;

            try
            {
                await using var connection = await _dbHelper.GetWrapperAsync();
                await using var reader = await connection.ExecuteReaderAsync("ValidateUser", CommandType.StoredProcedure,
                    new MySqlParameter("@p_Id", MySqlDbType.VarChar, 36) { Value = tokenId });

                if (!await reader.ReadAsync() || Convert.ToBoolean(reader["Revoked"]))
                    return (false, null, validationType);

                bool alreadyComplete = Convert.ToBoolean(reader["Complete"]);
                validationType = reader["VerifyType"].ToString() ?? string.Empty;
                string salt = reader["RefreshSalt"].ToString() ?? string.Empty;
                string storedHashedToken = reader["HashedToken"].ToString() ?? string.Empty;
                var hashedProvidedToken = PasswordHelper.HashPassword(providedTokenPart, salt);

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

                if (storedHashedToken != hashedProvidedToken ||
                    alreadyComplete ||
                    verifyRequest.TokenType != validationType)
                    return (false, user, validationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating verification token.");
                throw;
            }

            return (!string.IsNullOrEmpty(user?.Username), user, validationType);
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
            await using var connection = await _dbHelper.GetWrapperAsync();
            // Generate Refresh Token
            var salt = PasswordHelper.GenerateSalt();
            var hashedVerifyToken = PasswordHelper.HashPassword(verifyToken, salt);
            var tokenId = Guid.NewGuid().ToString(); // Unique identifier for the refresh token

            // Store Refresh Token in DB
            await connection.ExecuteNonQueryAsync("VerificationInsert", CommandType.StoredProcedure,
                new MySqlParameter("@p_Id", MySqlDbType.VarChar, 36) { Value = tokenId },
                new MySqlParameter("@p_VerifyType", MySqlDbType.VarChar, 20) { Value = verifyType },
                new MySqlParameter("@p_UserId", MySqlDbType.Int32) { Value = userId },
                new MySqlParameter("@p_HashedToken", MySqlDbType.VarChar, 255) { Value = hashedVerifyToken },
                new MySqlParameter("@p_Salt", MySqlDbType.VarChar, 255) { Value = salt },
                new MySqlParameter("@p_ExpiryDate", MySqlDbType.DateTime) { Value = DateTime.UtcNow.AddDays(7) }); // 7-day expiration

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

            await using var connection = await _dbHelper.GetWrapperAsync();

            var rowsAffectedParam = new MySqlParameter("@p_RowsAffected", MySqlDbType.Int32)
            {
                Direction = ParameterDirection.Output
            };

            await connection.ExecuteNonQueryAsync("RevokeVerifyToken", CommandType.StoredProcedure,
                new MySqlParameter("@p_UserId", MySqlDbType.Int32) { Value = userId },
                new MySqlParameter("@p_TokenId", MySqlDbType.VarChar, 36) { Value = tokenId ?? (object)DBNull.Value },
                new MySqlParameter("@p_VerifyType", MySqlDbType.VarChar, 20) { Value = verifyType },
                rowsAffectedParam);

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

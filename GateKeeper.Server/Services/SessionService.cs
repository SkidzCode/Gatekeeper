using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Services
{
    public class SessionService : ISessionService
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger<SessionService> _logger;
        private readonly IVerifyTokenService _verifyTokenService;

        public SessionService(/* IConfiguration configuration, */ IDbHelper dbHelper, ILogger<SessionService> logger, IVerifyTokenService verifyTokenService)
        {
            // var dbConfig = configuration.GetSection("DatabaseConfig").Get<DatabaseConfig>() ?? new DatabaseConfig(); // Removed
            _dbHelper = dbHelper;
            _logger = logger;
            _verifyTokenService = verifyTokenService;
        }

        /// <summary>
        /// Inserts a new Session using the SessionInsert stored procedure.
        /// </summary>
        public async Task InsertSession(SessionModel session)
        {
            await using var connection = await _dbHelper.GetWrapperAsync();
            await connection.ExecuteNonQueryAsync(
                "SessionInsert",
                CommandType.StoredProcedure,
                new MySqlParameter("@pId", MySqlDbType.VarChar, 36) { Value = session.Id },
                new MySqlParameter("@pUserId", MySqlDbType.Int32) { Value = session.UserId },
                new MySqlParameter("@pVerificationId", MySqlDbType.VarChar, 36) { Value = session.VerificationId },
                new MySqlParameter("@pExpiryDate", MySqlDbType.DateTime) { Value = session.ExpiryDate },
                new MySqlParameter("@pComplete", MySqlDbType.Bool) { Value = session.Complete },
                new MySqlParameter("@pRevoked", MySqlDbType.Bool) { Value = session.Revoked },
                new MySqlParameter("@pIpAddress", MySqlDbType.VarChar, 45) { Value = session.IpAddress },
                new MySqlParameter("@pUserAgent", MySqlDbType.VarChar, 255) { Value = session.UserAgent },
                new MySqlParameter("@pSessionData", MySqlDbType.Text) { Value = session.SessionData }
            );
        }

        /// <summary>
        /// Updates the VerificationId of an existing Session using SessionRefresh.
        /// </summary>
        public async Task<string> RefreshSession(int userId, string oldVerificationId, string newVerificationId)
        {
            await using var connection = await _dbHelper.GetWrapperAsync();
            var outputParam = new MySqlParameter("@pSessionId", MySqlDbType.VarChar, 36) { Direction = ParameterDirection.Output };

            await connection.ExecuteNonQueryAsync(
                "SessionRefresh",
                CommandType.StoredProcedure,
                new MySqlParameter("@pUserId", MySqlDbType.Int32) { Value = userId },
                new MySqlParameter("@pOldVerificationId", MySqlDbType.VarChar, 36) { Value = oldVerificationId },
                new MySqlParameter("@pNewVerificationId", MySqlDbType.VarChar, 36) { Value = newVerificationId },
                outputParam
            );

            return outputParam.Value.ToString();
        }

        /// <summary>
        /// Marks a session as Complete (logout) using SessionLogout.
        /// </summary>
        public async Task LogoutToken(string token, int userId)
        {
            var response = await _verifyTokenService.VerifyTokenAsync(new VerifyTokenRequest()
            {
                TokenType = "Refresh",
                VerificationCode = token
            });

            if (!response.IsVerified || userId != response.User.Id)
                return;

            string verificationId = token.Split('.')[0];

            await using var connection = await _dbHelper.GetWrapperAsync();
            await connection.ExecuteNonQueryAsync(
                "SessionLogout",
                CommandType.StoredProcedure,
                new MySqlParameter("@pVerificationId", MySqlDbType.VarChar, 36) { Value = verificationId }
            );
        }

        /// <summary>
        /// Marks a session as Complete (logout) using SessionLogout.
        /// </summary>
        public async Task LogoutSession(string verificationId, int userId)
        {
            await using var connection = await _dbHelper.GetWrapperAsync();
            await connection.ExecuteNonQueryAsync(
                "SessionIdLogout",
                CommandType.StoredProcedure,
                new MySqlParameter("@pSessionId", MySqlDbType.VarChar, 36) { Value = verificationId }
            );
        }

        /// <summary>
        /// Retrieves a list of all active sessions for a user (SessionActiveListForUser).
        /// </summary>
        public async Task<List<SessionModel>> GetActiveSessionsForUser(int userId)
        {
            var sessions = new List<SessionModel>();

            await using var connection = await _dbHelper.GetWrapperAsync();
            await using var reader = await connection.ExecuteReaderAsync(
                "SessionActiveListForUser",
                CommandType.StoredProcedure,
                new MySqlParameter("@pUserId", MySqlDbType.Int32) { Value = userId }
            );

            while (await reader.ReadAsync())
            {
                var session = new SessionModel
                {
                    Id = reader["Id"].ToString() ?? string.Empty,
                    UserId = Convert.ToInt32(reader["UserId"]),
                    VerificationId = reader["VerificationId"].ToString() ?? string.Empty,
                    ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"]),
                    Complete = Convert.ToBoolean(reader["Complete"]),
                    Revoked = Convert.ToBoolean(reader["Revoked"]),
                    CreatedAt = reader["CreatedAt"] as DateTime? ?? DateTime.MinValue,
                    UpdatedAt = reader["UpdatedAt"] as DateTime? ?? DateTime.MinValue,
                    IpAddress = reader["IpAddress"].ToString() ?? string.Empty,
                    UserAgent = reader["UserAgent"].ToString() ?? string.Empty,
                    SessionData = reader["SessionData"].ToString() ?? string.Empty,

                    // If you joined fields from Verification:
                    VerifyType = reader["VerifyType"]?.ToString(),
                    VerificationExpiryDate = reader["VerificationExpiryDate"] as DateTime?,
                    VerificationComplete = reader["VerificationComplete"] as bool?,
                    VerificationRevoked = reader["VerificationRevoked"] as bool?
                };
                sessions.Add(session);
            }

            return sessions;
        }

        /// <summary>
        /// Retrieves sessions for refresh tokens within the last 15 minutes (SessionListMostRecentActivity).
        /// </summary>
        public async Task<List<SessionModel>> GetMostRecentActivity()
        {
            var sessions = new List<SessionModel>();

            await using var connection = await _dbHelper.GetWrapperAsync();
            await using var reader = await connection.ExecuteReaderAsync(
                "SessionListMostRecentActivity",
                CommandType.StoredProcedure
            );

            while (await reader.ReadAsync())
            {
                var session = new SessionModel
                {
                    Id = reader["Id"].ToString() ?? string.Empty,
                    UserId = Convert.ToInt32(reader["UserId"]),
                    VerificationId = reader["VerificationId"].ToString() ?? string.Empty,
                    ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"]),
                    Complete = Convert.ToBoolean(reader["Complete"]),
                    Revoked = Convert.ToBoolean(reader["Revoked"]),
                    CreatedAt = reader["CreatedAt"] as DateTime? ?? DateTime.MinValue,
                    UpdatedAt = reader["UpdatedAt"] as DateTime? ?? DateTime.MinValue,
                    IpAddress = reader["IpAddress"].ToString() ?? string.Empty,
                    UserAgent = reader["UserAgent"].ToString() ?? string.Empty,
                    SessionData = reader["SessionData"].ToString() ?? string.Empty,

                    VerifyType = reader["VerifyType"]?.ToString(),
                    VerificationExpiryDate = reader["VerificationExpiryDate"] as DateTime?
                };
                sessions.Add(session);
            }

            return sessions;
        }
    }
}

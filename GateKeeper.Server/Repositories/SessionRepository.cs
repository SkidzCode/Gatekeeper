using Dapper;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GateKeeper.Server.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly IDbConnection _dbConnection;

        public SessionRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task InsertAsync(SessionModel session)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@pId", session.Id, DbType.String, ParameterDirection.Input, 36);
            parameters.Add("@pUserId", session.UserId, DbType.Int32);
            parameters.Add("@pVerificationId", session.VerificationId, DbType.String, ParameterDirection.Input, 36);
            parameters.Add("@pExpiryDate", session.ExpiryDate, DbType.DateTime);
            parameters.Add("@pComplete", session.Complete, DbType.Boolean);
            parameters.Add("@pRevoked", session.Revoked, DbType.Boolean);
            parameters.Add("@pIpAddress", session.IpAddress, DbType.String, ParameterDirection.Input, 45);
            parameters.Add("@pUserAgent", session.UserAgent, DbType.String, ParameterDirection.Input, 255);
            parameters.Add("@pSessionData", session.SessionData, DbType.String);

            await _dbConnection.ExecuteAsync("SessionInsert", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<string> RefreshAsync(int userId, string oldVerificationId, string newVerificationId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@pUserId", userId, DbType.Int32);
            parameters.Add("@pOldVerificationId", oldVerificationId, DbType.String, ParameterDirection.Input, 36);
            parameters.Add("@pNewVerificationId", newVerificationId, DbType.String, ParameterDirection.Input, 36);
            parameters.Add("@pSessionId", dbType: DbType.String, direction: ParameterDirection.Output, size: 36);

            await _dbConnection.ExecuteAsync("SessionRefresh", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<string>("@pSessionId");
        }

        public async Task LogoutByVerificationIdAsync(string verificationId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@pVerificationId", verificationId, DbType.String, ParameterDirection.Input, 36);
            await _dbConnection.ExecuteAsync("SessionLogout", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task LogoutBySessionIdAsync(string sessionId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@pSessionId", sessionId, DbType.String, ParameterDirection.Input, 36);
            await _dbConnection.ExecuteAsync("SessionIdLogout", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<List<SessionModel>> GetActiveByUserIdAsync(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@pUserId", userId, DbType.Int32);
            var sessions = await _dbConnection.QueryAsync<SessionModel>("SessionActiveListForUser", parameters, commandType: CommandType.StoredProcedure);
            return sessions.ToList();
        }

        public async Task<List<SessionModel>> GetMostRecentAsync()
        {
            var sessions = await _dbConnection.QueryAsync<SessionModel>("SessionListMostRecentActivity", commandType: CommandType.StoredProcedure);
            return sessions.ToList();
        }
    }
}

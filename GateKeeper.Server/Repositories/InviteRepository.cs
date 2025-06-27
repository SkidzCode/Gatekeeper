using Dapper;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using MySqlConnector;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GateKeeper.Server.Repositories
{
    public class InviteRepository : IInviteRepository
    {
        private readonly IDbConnection _dbConnection;

        public InviteRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<int> InsertInviteAsync(Invite invite)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_FromId", invite.FromId, DbType.Int32);
            parameters.Add("@p_ToName", invite.ToName, DbType.String, ParameterDirection.Input, 255);
            parameters.Add("@p_ToEmail", invite.ToEmail, DbType.String, ParameterDirection.Input, 255);
            parameters.Add("@p_VerificationId", invite.VerificationId, DbType.String, ParameterDirection.Input, 36);
            parameters.Add("@p_NotificationId", invite.NotificationId, DbType.Int32);
            parameters.Add("@last_id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _dbConnection.ExecuteAsync("InsertInvite", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@last_id");
        }

        public async Task<List<Invite>> GetInvitesByFromIdAsync(int fromId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_FromId", fromId, DbType.Int32);

            var invites = await _dbConnection.QueryAsync<Invite>("GetInvitesByFromId", parameters, commandType: CommandType.StoredProcedure);
            return invites.ToList();
        }
    }
}

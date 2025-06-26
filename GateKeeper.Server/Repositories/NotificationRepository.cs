using Dapper;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GateKeeper.Server.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IDbConnection _dbConnection;

        public NotificationRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<List<Notification>> GetAllAsync()
        {
            var notifications = await _dbConnection.QueryAsync<Notification>("NotificationsGetAll", commandType: CommandType.StoredProcedure);
            return notifications.ToList();
        }

        public async Task<List<Notification>> GetByRecipientIdAsync(int recipientId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_RecipientId", recipientId, DbType.Int32);
            var notifications = await _dbConnection.QueryAsync<Notification>("NotificationsGetUser", parameters, commandType: CommandType.StoredProcedure);
            return notifications.ToList();
        }

        public async Task<List<Notification>> GetNotSentAsync(DateTime currentTime)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_current_time", currentTime, DbType.DateTime);
            var notifications = await _dbConnection.QueryAsync<Notification>("NotificationsGetNotSent", parameters, commandType: CommandType.StoredProcedure);
            return notifications.ToList();
        }

        public async Task<int> InsertAsync(Notification notification)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_RecipientId", notification.RecipientId, DbType.Int32);
            parameters.Add("@p_FromId", notification.FromId, DbType.Int32);
            parameters.Add("@p_ToName", notification.ToName, DbType.String, ParameterDirection.Input, 255);
            parameters.Add("@p_ToEmail", notification.ToEmail, DbType.String, ParameterDirection.Input, 255);
            parameters.Add("@p_Channel", notification.Channel, DbType.String, ParameterDirection.Input, 10);
            parameters.Add("@p_URL", notification.URL, DbType.String, ParameterDirection.Input, 255);
            parameters.Add("@p_TokenType", notification.TokenType, DbType.String, ParameterDirection.Input, 50);
            parameters.Add("@p_Subject", notification.Subject, DbType.String);
            parameters.Add("@p_Message", notification.Message, DbType.String);
            parameters.Add("@p_ScheduledAt", notification.ScheduledAt, DbType.DateTime);
            // The stored procedure NotificationInsert is expected to do "SELECT LAST_INSERT_ID() AS new_id;"
            // Dapper's QuerySingleAsync<int> will correctly retrieve this.
            return await _dbConnection.QuerySingleAsync<int>("NotificationInsert", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateAsync(Notification notification)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_NotificationId", notification.Id, DbType.Int32);
            parameters.Add("@p_IsSent", notification.IsSent, DbType.Boolean);
            parameters.Add("@p_UpdatedAt", notification.UpdatedAt, DbType.DateTime);

            await _dbConnection.ExecuteAsync("NotificationUpdate", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}

using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;
using GateKeeper.Server.Models.Site;
using Newtonsoft.Json.Linq;

namespace GateKeeper.Server.Services
{
    public class InviteService : IInviteService
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger<InviteService> _logger;
        private readonly IVerifyTokenService _verificationService;
        private readonly INotificationService _notificationService;
        private readonly INotificationTemplateService _notificationTemplateService;
        
        public InviteService(IDbHelper dbHelper, ILogger<InviteService> logger, IVerifyTokenService veryTokenService, INotificationService notificationService, INotificationTemplateService notificationTemplateService)
        {
            _dbHelper = dbHelper;
            _logger = logger;
            _verificationService = veryTokenService;
            _notificationService = notificationService;
            _notificationTemplateService = notificationTemplateService;
        }

        public async Task<int> SendInvite(Invite invite)
        {
            var template = await _notificationTemplateService.GetNotificationTemplateByNameAsync("InviteUserTemplate");
            if (template == null)
            {
                _logger.LogError("Invite template not found");
                return 0;
            }
            
            var response = await _notificationService.InsertNotificationAsync(new Notification()
            {
                Channel = "Email",
                Message = template.Body,
                Subject = template.Subject,
                RecipientId = 0,
                TokenType = template.TokenType,
                URL = invite.Website,
                FromId = invite.FromId,
                ToEmail = invite.ToEmail,
                ToName = invite.ToName
            });

            invite.NotificationId = response.NotificationId;
            invite.VerificationId = response.VerificationId;
            int InviteId = await InsertInvite(invite);

            return InviteId;
        }

        /// <summary>
        /// Calls the "InsertInvite" stored procedure to add a new Invite row.
        /// </summary>
        /// <param name="invite">The invite details to insert.</param>
        /// <returns>The new invite's Id.</returns>
        public async Task<int> InsertInvite(Invite invite)
        {
            await using var connection = await _dbHelper.GetWrapperAsync();

            var outputParameters = await connection.ExecuteNonQueryWithOutputAsync(
                "InsertInvite",
                CommandType.StoredProcedure,
                new MySqlParameter("@p_FromId", MySqlDbType.Int32) { Value = invite.FromId },
                new MySqlParameter("@p_ToName", MySqlDbType.VarChar, 255) { Value = invite.ToName },
                new MySqlParameter("@p_ToEmail", MySqlDbType.VarChar, 255) { Value = invite.ToEmail },
                new MySqlParameter("@p_VerificationId", MySqlDbType.VarChar, 36) { Value = invite.VerificationId },
                new MySqlParameter("@p_NotificationId", MySqlDbType.Int32)
                {
                    Value = invite.NotificationId ?? (object)DBNull.Value
                },
                new MySqlParameter("@last_id", MySqlDbType.Int32) { Direction = ParameterDirection.Output }
            );

            var newId = 0;
            if (outputParameters["@last_id"] != DBNull.Value)
            {
                newId = Convert.ToInt32(outputParameters["@last_id"]);
            }

            return newId;
        }


        /// <summary>
        /// Calls the "GetInvitesByFromId" stored procedure and retrieves each record.
        /// </summary>
        /// <param name="fromId">The Id of the user who sent the invite.</param>
        /// <returns>A list of invites with status details.</returns>
        public async Task<List<Invite>> GetInvitesByFromId(int fromId)
        {
            var invites = new List<Invite>();
            await using var connection = await _dbHelper.GetWrapperAsync();

            await using var reader = await connection.ExecuteReaderAsync(
                "GetInvitesByFromId",
                CommandType.StoredProcedure,
                new MySqlParameter("@p_FromId", MySqlDbType.Int32) { Value = fromId }
            );

            while (await reader.ReadAsync())
            {
                var invite = new Invite
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    FromId = Convert.ToInt32(reader["FromId"]),
                    ToName = reader["ToName"].ToString(),
                    ToEmail = reader["ToEmail"].ToString(),
                    Created = reader["Created"] != DBNull.Value
                              ? Convert.ToDateTime(reader["Created"])
                              : DateTime.MinValue,
                    // The four status fields from the CASE statements in the SP
                    IsExpired = Convert.ToBoolean(reader["IsExpired"]),
                    IsRevoked = Convert.ToBoolean(reader["IsRevoked"]),
                    IsComplete = Convert.ToBoolean(reader["IsComplete"]),
                    IsSent = Convert.ToBoolean(reader["IsSent"])
                };
                invites.Add(invite);
            }
            return invites;
        }
    }
}

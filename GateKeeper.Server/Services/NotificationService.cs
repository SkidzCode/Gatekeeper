using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using MySqlConnector;
using GateKeeper.Server.Interface; // For IDBHelper, IMySqlConnectorWrapper, etc.
using GateKeeper.Server.Models;
using GateKeeper.Server.Models.Account.Notifications;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Models.Site; // For Notification model

namespace GateKeeper.Server.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IDbHelper _dbHelper;
        private readonly IEmailService _emailService;
        // private readonly IConfiguration _configuration; // Removed
        private readonly IUserService _userService;
        private readonly IVerifyTokenService _verifyTokenService;

        public NotificationService(
            IDbHelper dbHelper, IEmailService emailService, 
            /* IConfiguration configuration, */ IUserService userService, // IConfiguration parameter removed
            IVerifyTokenService verifyTokenService)
        {
            _dbHelper = dbHelper;
            _emailService = emailService;
            // _configuration = configuration; // Removed
            _userService = userService;
            _verifyTokenService = verifyTokenService;
        }

        /// <summary>
        /// List all notifications.
        /// </summary>
        /// <returns>List of all notifications in the database.</returns>
        public async Task<List<Notification>> GetAllNotificationsAsync()
        {
            var notifications = new List<Notification>();

            // Acquire the connection wrapper
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            // Call the stored procedure
            await using var reader = await wrapper.ExecuteReaderAsync(
                "NotificationsGetAll",
                CommandType.StoredProcedure
            );

            // Read the results
            while (await reader.ReadAsync())
            {
                notifications.Add(MapNotification(reader));
            }

            return notifications;
        }

        /// <summary>
        /// List all notifications for a specific user (recipient).
        /// </summary>
        /// <param name="recipientId">User's ID.</param>
        /// <returns>List of notifications for the specified user.</returns>
        public async Task<List<Notification>> GetNotificationsByRecipientAsync(int recipientId)
        {
            var notifications = new List<Notification>();

            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var param = new MySqlParameter("@p_RecipientId", MySqlDbType.Int32)
            {
                Value = recipientId
            };

            await using var reader = await wrapper.ExecuteReaderAsync(
                "NotificationsGetUser",
                CommandType.StoredProcedure,
                param
            );

            while (await reader.ReadAsync())
            {
                notifications.Add(MapNotification(reader));
            }

            return notifications;
        }

        /// <summary>
        /// List all notifications that are not sent yet and scheduled to be sent
        /// on or before <paramref name="currentTime"/>.
        /// </summary>
        /// <param name="currentTime">The current time to compare against.</param>
        /// <returns>List of notifications that are ready to be sent.</returns>
        public async Task<List<Notification>> GetNotSentNotificationsAsync(DateTime currentTime)
        {
            var notifications = new List<Notification>();

            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var param = new MySqlParameter("@p_current_time", MySqlDbType.DateTime)
            {
                Value = currentTime
            };

            await using var reader = await wrapper.ExecuteReaderAsync(
                "NotificationsGetNotSent",
                CommandType.StoredProcedure,
                param
            );

            while (await reader.ReadAsync())
            {
                notifications.Add(MapNotification(reader));
            }

            return notifications;
        }

        /// <summary>
        /// Inserts a new notification and returns the newly generated ID.
        /// </summary>
        /// <param name="notification">Notification object to insert.</param>
        /// <returns>The newly inserted notification's ID.</returns>
        public async Task<NotificationInsertResponse> InsertNotificationAsync(Notification notification)
        {
            var response = new NotificationInsertResponse();
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            notification.Message = notification.Message.Replace("{{URL}}", notification.URL);
            notification.Subject = notification.Subject.Replace("{{URL}}", notification.URL);

            User userFrom = await _userService.GetUser(notification.FromId);
            notification.Message = notification.Message.Replace("{{From_First_Name}}", userFrom.FirstName);
            notification.Message = notification.Message.Replace("{{From_Last_Name}}", userFrom.LastName);
            notification.Message = notification.Message.Replace("{{From_Email}}", userFrom.Email);
            notification.Message = notification.Message.Replace("{{From_Username}}", userFrom.Username);


            notification.Message = notification.Message.Replace("{{Email}}", notification.ToEmail);

            User? userTo = null;
            if (notification.RecipientId > 0)
            {
                userTo = await _userService.GetUser(notification.RecipientId);
                if (userTo != null)
                {
                    if (string.IsNullOrEmpty(notification.ToName))
                        notification.ToName = userTo.FirstName + " " + userTo.LastName;
                    if (string.IsNullOrEmpty(notification.ToEmail))
                        notification.ToEmail = userTo.Email;
                }
            }


            if (notification.ToName.Contains(" "))
            {
                string fristName = notification.ToName.Split(" ")[0];
                string lastName = notification.ToName.Split(" ")[1];
                notification.Message = notification.Message.Replace("{{First_Name}}", fristName);
                notification.Message = notification.Message.Replace("{{Last_Name}}", lastName);
                notification.Subject = notification.Subject.Replace("{{First_Name}}", fristName);
                notification.Subject = notification.Subject.Replace("{{Last_Name}}", lastName);
            }
            else
            {
                notification.Message = notification.Message.Replace("{{First_Name}}", notification.ToName);
                notification.Message = notification.Message.Replace("{{Last_Name}}", "");
                notification.Subject = notification.Subject.Replace("{{First_Name}}", notification.ToName);
                notification.Subject = notification.Subject.Replace("{{Last_Name}}", "");
            }

            string verificationCode = string.Empty;
            if (userTo != null)
            {
                notification.Subject = notification.Subject.Replace("{{Username}}", userTo.Username);

                if (!string.IsNullOrEmpty(notification.TokenType) &&
                    notification.Message.Contains("{{Verification_Code}}"))
                {
                    verificationCode = await _verifyTokenService.GenerateTokenAsync(userTo.Id, notification.TokenType);
                }
            }
            else if (!string.IsNullOrEmpty(notification.TokenType) && notification.Message.Contains("{{Verification_Code}}"))
            {
                verificationCode = await _verifyTokenService.GenerateTokenAsync(userFrom.Id, notification.TokenType);
            }

            notification.Message = notification.Message.Replace("{{Verification_Code}}", WebUtility.UrlEncode(verificationCode));

            if (verificationCode.Length > 0)
            {
                response.VerificationId = verificationCode.Split('.')[0];
            }


            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@p_RecipientId", MySqlDbType.Int32)
                {
                    Value = notification.RecipientId
                },
                new MySqlParameter("@p_FromId", MySqlDbType.Int32)
                {
                    Value = notification.FromId
                },
                new MySqlParameter("@p_ToName", MySqlDbType.VarChar, 255)
                {
                    Value = (object?)notification.ToName ?? DBNull.Value
                },
                new MySqlParameter("@p_ToEmail", MySqlDbType.VarChar, 255)
                {
                    Value = (object?)notification.ToEmail ?? DBNull.Value
                },
                new MySqlParameter("@p_Channel", MySqlDbType.VarChar, 10)
                {
                    Value = notification.Channel
                },
                new MySqlParameter("@p_URL", MySqlDbType.VarChar, 255)
                {
                    Value = (object?)notification.URL ?? DBNull.Value
                },
                new MySqlParameter("@p_TokenType", MySqlDbType.VarChar, 50)
                {
                    Value = (object?)notification.TokenType ?? DBNull.Value
                },
                new MySqlParameter("@p_Subject", MySqlDbType.Text)
                {
                    Value = notification.Subject
                },
                new MySqlParameter("@p_Message", MySqlDbType.Text)
                {
                    Value = notification.Message
                },
                new MySqlParameter("@p_ScheduledAt", MySqlDbType.DateTime)
                {
                    Value = (object?)notification.ScheduledAt ?? DBNull.Value
                }
            };

            // We expect a result set with a single row containing `new_id`.
            await using var reader = await wrapper.ExecuteReaderAsync(
                "NotificationInsert",
                CommandType.StoredProcedure,
                parameters.ToArray()
            );

            
            if (await reader.ReadAsync())
            {
                // Since we did SELECT LAST_INSERT_ID() AS new_id in the procedure
                response.NotificationId = Convert.ToInt32(reader["new_id"]);
            }

            return response;
        }

        /// <summary>
        /// Helper method to map a data reader row to a Notification object.
        /// </summary>
        private Notification MapNotification(IMySqlDataReaderWrapper reader)
        {
            var notification = new Notification
            {
                Id = reader.GetInt32("Id"),
                RecipientId = reader.GetInt32("RecipientId"),
                FromId = reader.GetInt32("FromId"),
                ToEmail = reader.GetString("ToEmail"),
                ToName  = reader.GetString("ToName"),
                Channel = reader.GetString("Channel"),
                URL = reader.GetString("URL"),
                TokenType = reader.GetString("TokenType"),
                Subject = reader.GetString("Subject"),
                Message = reader.GetString("Message"),
                IsSent = Convert.ToBoolean(reader["IsSent"]),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                UpdatedAt = reader.GetDateTime("UpdatedAt")
            };

            int scheduledAtOrdinal = reader.GetOrdinal("ScheduledAt");
            if (!reader.IsDBNull(scheduledAtOrdinal))
            {
                notification.ScheduledAt = reader.GetDateTime("ScheduledAt");
            }
            else
            {
                notification.ScheduledAt = null;
            }

            return notification;
        }

        public async Task ProcessPendingNotificationsAsync()
        {
            var currentTime = DateTime.UtcNow;
            var pendingNotifications = await GetNotSentNotificationsAsync(currentTime);

            foreach (var notification in pendingNotifications)
            {
                if (notification.Channel != "email") continue;
                
                var fromUser = await _userService.GetUser(notification.FromId);
                await _emailService.SendEmailAsync(notification.ToEmail, notification.ToName, fromUser.Username, notification.Subject, notification.Message);

                // Mark the notification as sent
                notification.IsSent = true;
                notification.UpdatedAt = DateTime.UtcNow;
                await UpdateNotificationAsync(notification);
            }
        }

        private async Task UpdateNotificationAsync(Notification notification)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@p_NotificationId", notification.Id),
                new MySqlParameter("@p_IsSent", notification.IsSent),
                new MySqlParameter("@p_UpdatedAt", notification.UpdatedAt)
            };

            await wrapper.ExecuteNonQueryAsync(
                commandText: "NotificationUpdate",
                commandType: CommandType.StoredProcedure,
                parameters: parameters
            );
        }
    }
}

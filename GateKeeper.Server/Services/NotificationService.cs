using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using GateKeeper.Server.Interface; // For IDBHelper, IMySqlConnectorWrapper, etc.
using GateKeeper.Server.Models;
using GateKeeper.Server.Models.Site; // For Notification model

namespace GateKeeper.Server.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IDbHelper _dbHelper;

        public NotificationService(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
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
                "sp_list_all_notifications",
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

            var param = new MySqlParameter("@p_recipient_id", MySqlDbType.Int32)
            {
                Value = recipientId
            };

            await using var reader = await wrapper.ExecuteReaderAsync(
                "sp_list_notifications_by_user",
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
                "sp_list_notifications_not_sent",
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
        public async Task<int> InsertNotificationAsync(Notification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@p_recipient_id", MySqlDbType.Int32)
                {
                    Value = notification.RecipientId
                },
                new MySqlParameter("@p_channel", MySqlDbType.VarChar, 10)
                {
                    Value = notification.Channel
                },
                new MySqlParameter("@p_message", MySqlDbType.Text)
                {
                    Value = notification.Message
                },
                new MySqlParameter("@p_scheduled_at", MySqlDbType.DateTime)
                {
                    Value = (object?)notification.ScheduledAt ?? DBNull.Value
                }
            };

            // We expect a result set with a single row containing `new_id`.
            await using var reader = await wrapper.ExecuteReaderAsync(
                "sp_insert_notification",
                CommandType.StoredProcedure,
                parameters.ToArray()
            );

            var newId = 0;
            if (await reader.ReadAsync())
            {
                // Since we did SELECT LAST_INSERT_ID() AS new_id in the procedure
                newId = Convert.ToInt32(reader["new_id"]);
            }

            return newId;
        }

        /// <summary>
        /// Helper method to map a data reader row to a Notification object.
        /// </summary>
        private Notification MapNotification(IMySqlDataReaderWrapper reader)
        {
            var notification = new Notification
            {
                Id = reader.GetInt32("id"),
                RecipientId = reader.GetInt32("recipient_id"),
                Channel = reader.GetString("channel"),
                Message = reader.GetString("message"),
                IsSent = Convert.ToBoolean(reader["is_sent"]),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };

            int scheduledAtOrdinal = reader.GetOrdinal("scheduled_at");
            if (!reader.IsDBNull(scheduledAtOrdinal))
            {
                notification.ScheduledAt = reader.GetDateTime("scheduled_at");
            }
            else
            {
                notification.ScheduledAt = null;
            }

            return notification;
        }
    }
}

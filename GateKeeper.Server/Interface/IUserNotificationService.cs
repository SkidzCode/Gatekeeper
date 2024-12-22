using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for managing user notifications and admin alerts.
    /// </summary>
    public interface IUserNotificationService
    {
        /// <summary>
        /// Sends a notification to the user via email or SMS.
        /// </summary>
        /// <param name="userId">The ID of the user to notify.</param>
        /// <param name="notificationRequest">The notification details.</param>
        /// <returns>Whether the notification was successfully sent.</returns>
        Task<bool> SendUserNotificationAsync(int userId, NotificationRequest notificationRequest);

        /// <summary>
        /// Sends an alert to admins regarding suspicious activity or errors.
        /// </summary>
        /// <param name="adminAlertRequest">The admin alert details.</param>
        /// <returns>Whether the alert was successfully sent.</returns>
        Task<bool> SendAdminAlertAsync(AdminAlertRequest adminAlertRequest);

        /// <summary>
        /// Retrieves the notification preferences for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The user's notification preferences.</returns>
        Task<NotificationPreferences> GetNotificationPreferencesAsync(int userId);

        /// <summary>
        /// Updates the notification preferences for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="preferences">The updated notification preferences.</param>
        /// <returns>Whether the update was successful.</returns>
        Task<bool> UpdateNotificationPreferencesAsync(int userId, NotificationPreferences preferences);

        /// <summary>
        /// Logs user notification history for auditing purposes.
        /// </summary>
        /// <param name="logEntry">The notification log entry to save.</param>
        /// <returns>Whether the log entry was successfully saved.</returns>
        Task<bool> LogNotificationAsync(NotificationLogEntry logEntry);

        /// <summary>
        /// Retrieves the notification history for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="filter">Filters for retrieving notification history.</param>
        /// <returns>A list of notification history entries.</returns>
        Task<IEnumerable<NotificationHistory>> GetNotificationHistoryAsync(int userId, NotificationHistoryFilter filter);

        /// <summary>
        /// Retrieves the admin alert history for monitoring and auditing purposes.
        /// </summary>
        /// <param name="filter">Filters for retrieving admin alert history.</param>
        /// <returns>A list of admin alert history entries.</returns>
        Task<IEnumerable<AdminAlertHistory>> GetAdminAlertHistoryAsync(AdminAlertHistoryFilter filter);
    }
}

using GateKeeper.Server.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Models.Site;
using GateKeeper.Server.Models.Account.Notifications;

namespace GateKeeper.Server.Interface
{
    public interface INotificationService
    {
        /// <summary>
        /// Returns a list of all notifications in the database.
        /// </summary>
        Task<List<Notification>> GetAllNotificationsAsync();

        /// <summary>
        /// Returns a list of all notifications for a specific user (recipient).
        /// </summary>
        /// <param name="recipientId">User's ID.</param>
        Task<List<Notification>> GetNotificationsByRecipientAsync(int recipientId);

        /// <summary>
        /// Returns a list of all notifications that are not sent yet and are 
        /// scheduled to be sent on or before the specified current time.
        /// </summary>
        /// <param name="currentTime">Time used to filter notifications to be sent.</param>
        Task<List<Notification>> GetNotSentNotificationsAsync(DateTime currentTime);

        /// <summary>
        /// Inserts a new notification and returns the newly inserted notification's ID.
        /// </summary>
        /// <param name="notification">Notification object to insert.</param>
        Task<NotificationInsertResponse> InsertNotificationAsync(Notification notification);

        /// <summary>
        /// Looks up notifications that need to be sent and sends them.
        /// </summary>
        Task ProcessPendingNotificationsAsync();
    }
}
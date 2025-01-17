using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using GateKeeper.Server.Interface;  // For INotificationService
using GateKeeper.Server.Models;
using GateKeeper.Server.Models.Site; // For Notification model

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// API controller for handling notification-related operations:
    /// listing, listing by user, listing not-sent, and inserting new notifications.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        /// <summary>
        /// Constructor for the NotificationController.
        /// </summary>
        /// <param name="notificationService">Notification service dependency.</param>
        /// <param name="logger">Logger dependency.</param>
        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all notifications in the system.
        /// </summary>
        /// <returns>A list of notifications.</returns>
        [HttpGet]
        [Authorize] // Optional: Add your desired authorization policy
        public async Task<IActionResult> GetAllNotifications()
        {
            try
            {
                var notifications = await _notificationService.GetAllNotificationsAsync();
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error occurred while fetching all notifications: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Retrieves all notifications for a specific user (by recipient ID).
        /// </summary>
        /// <param name="recipientId">The user/recipient ID.</param>
        /// <returns>A list of notifications for the specified user.</returns>
        [HttpGet("user/{recipientId:int}")]
        [Authorize] // Optional: Add your desired authorization policy
        public async Task<IActionResult> GetNotificationsByRecipient(int recipientId)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsByRecipientAsync(recipientId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error retrieving notifications for user {recipientId}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Retrieves all notifications that are not yet sent 
        /// and need to be sent on or before the current time (UTC).
        /// </summary>
        /// <returns>A list of notifications ready to be sent.</returns>
        [HttpGet("not-sent")]
        [Authorize] // Optional: Add your desired authorization policy
        public async Task<IActionResult> GetNotSentNotifications()
        {
            try
            {
                // Here, we pass DateTime.UtcNow. If you want, you could allow a query param or body param.
                var notSentNotifications = await _notificationService.GetNotSentNotificationsAsync(DateTime.UtcNow);
                return Ok(notSentNotifications);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error retrieving not-sent notifications: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Inserts a new notification.
        /// </summary>
        /// <param name="notification">The notification object to insert.</param>
        /// <returns>The newly inserted notification ID.</returns>
        [HttpPost]
        [Authorize] // Optional: Add your desired authorization policy or roles
        public async Task<IActionResult> InsertNotification([FromBody] Notification notification)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                notification.FromId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var newId = await _notificationService.InsertNotificationAsync(notification);
                return Ok(new
                {
                    message = "Notification created successfully.",
                    newNotificationId = newId
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error creating new notification: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account.Notifications;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Models.Site;
using Microsoft.Extensions.Logging; // Added for logging

namespace GateKeeper.Server.Services.Site
{
    public class NotificationService(
        INotificationRepository notificationRepository,
        IEmailService emailService,
        IUserService userService,
        IVerifyTokenService verifyTokenService,
        ILogger<NotificationService> logger)
        : INotificationService
    {

        public async Task<List<Notification>> GetAllNotificationsAsync()
        {
            logger.LogInformation("Fetching all notifications.");
            return await notificationRepository.GetAllAsync();
        }

        public async Task<List<Notification>> GetNotificationsByRecipientAsync(int recipientId)
        {
            logger.LogInformation("Fetching notifications for recipient ID: {RecipientId}", recipientId);
            return await notificationRepository.GetByRecipientIdAsync(recipientId);
        }

        public async Task<List<Notification>> GetNotSentNotificationsAsync(DateTime currentTime)
        {
            logger.LogInformation("Fetching not sent notifications scheduled before or at: {CurrentTime}", currentTime);
            return await notificationRepository.GetNotSentAsync(currentTime);
        }

        public async Task<NotificationInsertResponse> InsertNotificationAsync(Notification notification)
        {
            if (notification == null)
            {
                logger.LogError("Attempted to insert a null notification.");
                throw new ArgumentNullException(nameof(notification));
            }
            logger.LogInformation("Inserting notification for recipient: {ToEmail}, Subject: {Subject}", notification.ToEmail, notification.Subject);

            var response = new NotificationInsertResponse();

            // Replace URL placeholder first
            if (!string.IsNullOrEmpty(notification.URL))
            {
                notification.Message = notification.Message.Replace("{{URL}}", notification.URL);
                notification.Subject = notification.Subject.Replace("{{URL}}", notification.URL);
            }

            // Get FromUser details
            User userFrom = await userService.GetUser(notification.FromId);
            if (userFrom == null)
            {
                logger.LogError("FromUser with ID {FromId} not found for notification.", notification.FromId);
                // Handle error appropriately, perhaps throw or return an error response
                throw new InvalidOperationException($"FromUser with ID {notification.FromId} not found.");
            }
            notification.Message = notification.Message.Replace("{{From_First_Name}}", userFrom.FirstName);
            notification.Message = notification.Message.Replace("{{From_Last_Name}}", userFrom.LastName);
            notification.Message = notification.Message.Replace("{{From_Email}}", userFrom.Email);
            notification.Message = notification.Message.Replace("{{From_Username}}", userFrom.Username);

            // Replace general Email placeholder with ToEmail
            notification.Message = notification.Message.Replace("{{Email}}", notification.ToEmail);

            // Get ToUser details if RecipientId is provided
            User userTo = null;
            if (notification.RecipientId > 0)
            {
                userTo = await userService.GetUser(notification.RecipientId);
                if (userTo != null)
                {
                    if (string.IsNullOrEmpty(notification.ToName))
                        notification.ToName = $"{userTo.FirstName} {userTo.LastName}".Trim();
                    if (string.IsNullOrEmpty(notification.ToEmail))
                        notification.ToEmail = userTo.Email;
                }
                else
                {
                    logger.LogWarning("RecipientUser with ID {RecipientId} not found, but RecipientId was provided.", notification.RecipientId);
                }
            }

            // Replace Name placeholders
            if (!string.IsNullOrEmpty(notification.ToName))
            {
                var nameParts = notification.ToName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                string firstName = nameParts.Length > 0 ? nameParts[0] : notification.ToName; // Fallback to full name if no space
                string lastName = nameParts.Length > 1 ? nameParts[1] : "";
                notification.Message = notification.Message.Replace("{{First_Name}}", firstName);
                notification.Message = notification.Message.Replace("{{Last_Name}}", lastName);
                notification.Subject = notification.Subject.Replace("{{First_Name}}", firstName);
                notification.Subject = notification.Subject.Replace("{{Last_Name}}", lastName);
            }


            // Generate verification code if needed
            string verificationCode = string.Empty;
            if (!string.IsNullOrEmpty(notification.TokenType) && notification.Message.Contains("{{Verification_Code}}"))
            {
                int userIdForToken = userTo?.Id ?? userFrom.Id; // Prioritize ToUser for token, fallback to FromUser
                if (userTo == null && notification.RecipientId > 0)
                {
                    logger.LogWarning("Token generation for notification: ToUser was specified by RecipientId {RecipientId} but not found. Token will be generated for FromUser ID {FromId}.", notification.RecipientId, userFrom.Id);
                }
                else if (userTo == null)
                {
                    logger.LogInformation("Token generation for notification: ToUser is not specified or not found. Token will be generated for FromUser ID {FromId}.", userFrom.Id);
                }

                verificationCode = await verifyTokenService.GenerateTokenAsync(userIdForToken, notification.TokenType);
                notification.Message = notification.Message.Replace("{{Verification_Code}}", WebUtility.UrlEncode(verificationCode));

                if (!string.IsNullOrEmpty(verificationCode) && verificationCode.Contains("."))
                {
                    response.VerificationId = verificationCode.Split('.')[0];
                }
                else if (!string.IsNullOrEmpty(verificationCode))
                {
                    response.VerificationId = verificationCode; // Handle cases where token might not have a dot
                }
            }

            if (userTo != null) // Only replace {{Username}} if userTo is resolved
            {
                notification.Subject = notification.Subject.Replace("{{Username}}", userTo.Username);
            }


            response.NotificationId = await notificationRepository.InsertAsync(notification);
            logger.LogInformation("Notification inserted with ID: {NotificationId}", response.NotificationId);
            return response;
        }

        public async Task ProcessPendingNotificationsAsync()
        {
            var currentTime = DateTime.UtcNow;
            logger.LogInformation("Processing pending notifications up to: {CurrentTime}", currentTime);
            var pendingNotifications = await GetNotSentNotificationsAsync(currentTime);

            if (!pendingNotifications.Any())
            {
                logger.LogInformation("No pending notifications to process.");
                return;
            }

            foreach (var notification in pendingNotifications)
            {
                logger.LogInformation("Processing notification ID: {NotificationId}, Channel: {Channel}", notification.Id, notification.Channel);
                if (notification.Channel.Equals("email", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var fromUser = await userService.GetUser(notification.FromId);
                        if (fromUser == null)
                        {
                            logger.LogError("FromUser not found for notification ID: {NotificationId}. Skipping email.", notification.Id);
                            continue;
                        }
                        await emailService.SendEmailAsync(notification.ToEmail, notification.ToName, fromUser.Username, notification.Subject, notification.Message);
                        logger.LogInformation("Email sent for notification ID: {NotificationId}", notification.Id);

                        notification.IsSent = true;
                        notification.UpdatedAt = DateTime.UtcNow;
                        await notificationRepository.UpdateAsync(notification);
                        logger.LogInformation("Notification ID: {NotificationId} marked as sent.", notification.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing email notification ID: {NotificationId}", notification.Id);
                        // Decide on error handling: retry, mark as failed, etc.
                    }
                }
                else
                {
                    logger.LogWarning("Unsupported notification channel '{Channel}' for notification ID: {NotificationId}", notification.Channel, notification.Id);
                }
            }
            logger.LogInformation("Finished processing {Count} pending notifications.", pendingNotifications.Count);
        }
    }
}

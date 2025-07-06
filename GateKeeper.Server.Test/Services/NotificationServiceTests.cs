using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Models.Account.Notifications;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;
using GateKeeper.Server.Services.Site;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class NotificationServiceTests
    {
        private Mock<INotificationRepository> _mockNotificationRepository;
        private Mock<IEmailService> _mockEmailService;
        private Mock<IUserService> _mockUserService;
        private Mock<IVerifyTokenService> _mockVerifyTokenService;
        private Mock<ILogger<NotificationService>> _mockLogger;
        private NotificationService _notificationService;

        private Notification CreateTestNotification(int id = 1, int recipientId = 1, int fromId = 2,
                                                    string toEmail = "recipient@example.com", string toName = "Recipient Name",
                                                    string channel = "email", string url = "http://example.com/action",
                                                    string tokenType = "TestToken", string subject = "Test Subject",
                                                    string message = "Test Message {{First_Name}}", bool isSent = false,
                                                    DateTime? scheduledAt = null, DateTime? createdAt = null, DateTime? updatedAt = null)
        {
            return new Notification
            {
                Id = id, RecipientId = recipientId, FromId = fromId, ToEmail = toEmail, ToName = toName,
                Channel = channel, URL = url, TokenType = tokenType, Subject = subject, Message = message,
                IsSent = isSent, ScheduledAt = scheduledAt, CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-1),
                UpdatedAt = updatedAt ?? DateTime.UtcNow
            };
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _mockNotificationRepository = new Mock<INotificationRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockUserService = new Mock<IUserService>();
            _mockVerifyTokenService = new Mock<IVerifyTokenService>();
            _mockLogger = new Mock<ILogger<NotificationService>>();

            _notificationService = new NotificationService(
                _mockNotificationRepository.Object,
                _mockEmailService.Object,
                _mockUserService.Object,
                _mockVerifyTokenService.Object,
                _mockLogger.Object
            );
        }

        #region GetAllNotificationsAsync Tests
        [TestMethod]
        public async Task GetAllNotificationsAsync_ReturnsListOfNotifications()
        {
            var notificationsData = new List<Notification> { CreateTestNotification(1), CreateTestNotification(2) };
            _mockNotificationRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(notificationsData);

            var result = await _notificationService.GetAllNotificationsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(notificationsData.Count, result.Count);
            _mockNotificationRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
        }
        #endregion

        #region GetNotificationsByRecipientAsync Tests
        [TestMethod]
        public async Task GetNotificationsByRecipientAsync_ReturnsUserNotifications()
        {
            var recipientId = 1;
            var notificationsData = new List<Notification> { CreateTestNotification(1, recipientId: recipientId) };
            _mockNotificationRepository.Setup(repo => repo.GetByRecipientIdAsync(recipientId)).ReturnsAsync(notificationsData);

            var result = await _notificationService.GetNotificationsByRecipientAsync(recipientId);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(notificationsData[0].Id, result[0].Id);
            _mockNotificationRepository.Verify(repo => repo.GetByRecipientIdAsync(recipientId), Times.Once);
        }
        #endregion

        #region GetNotSentNotificationsAsync Tests
        [TestMethod]
        public async Task GetNotSentNotificationsAsync_ReturnsNotSentNotifications()
        {
            var currentTime = DateTime.UtcNow;
            var notificationsData = new List<Notification> { CreateTestNotification(isSent: false) };
            _mockNotificationRepository.Setup(repo => repo.GetNotSentAsync(currentTime)).ReturnsAsync(notificationsData);

            var result = await _notificationService.GetNotSentNotificationsAsync(currentTime);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(notificationsData[0].Id, result[0].Id);
            _mockNotificationRepository.Verify(repo => repo.GetNotSentAsync(currentTime), Times.Once);
        }
        #endregion

        #region InsertNotificationAsync Tests
        [TestMethod]
        public async Task InsertNotificationAsync_BasicInsert_ReturnsNewIdAndVerificationId()
        {
            var notification = CreateTestNotification(fromId: 1, recipientId: 2, toEmail: "test@test.com", toName: "Test Recipient",
                                                     url: "http://example.com/basic", tokenType: "BasicToken",
                                                     message: "Hello {{First_Name}}, verify with {{Verification_Code}} at {{URL}} from {{From_First_Name}}");
            var expectedNewId = 123;
            var expectedVerificationToken = "tokenABC.def";
            var expectedVerificationId = "tokenABC";

            _mockUserService.Setup(s => s.GetUser(notification.FromId))
                            .ReturnsAsync(new User { Id = notification.FromId, FirstName = "Sender", LastName = "User", Email = "sender@example.com", Username = "senderUser" });
            _mockUserService.Setup(s => s.GetUser(notification.RecipientId))
                            .ReturnsAsync(new User { Id = notification.RecipientId, FirstName = "Test", LastName = "Recipient", Email = "test@test.com", Username = "testUser" });
            _mockVerifyTokenService.Setup(s => s.GenerateTokenAsync(notification.RecipientId, notification.TokenType))
                                   .ReturnsAsync(expectedVerificationToken);
            _mockNotificationRepository.Setup(repo => repo.InsertAsync(It.IsAny<Notification>()))
                                       .ReturnsAsync(expectedNewId)
                                       .Callback<Notification>(n => {
                                           Assert.IsTrue(n.Message.Contains("Test")); // {{First_Name}}
                                           Assert.IsTrue(n.Message.Contains(WebUtility.UrlEncode(expectedVerificationToken))); // {{Verification_Code}}
                                           Assert.IsTrue(n.Message.Contains(notification.URL)); // {{URL}}
                                           Assert.IsTrue(n.Message.Contains("Sender")); // {{From_First_Name}}
                                       });

            var result = await _notificationService.InsertNotificationAsync(notification);

            Assert.AreEqual(expectedNewId, result.NotificationId);
            Assert.AreEqual(expectedVerificationId, result.VerificationId);
            _mockNotificationRepository.Verify(repo => repo.InsertAsync(It.IsAny<Notification>()), Times.Once);
        }

        [TestMethod]
        public async Task InsertNotificationAsync_NullNotification_ThrowsArgumentNullException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _notificationService.InsertNotificationAsync(null));
        }

        [TestMethod]
        public async Task InsertNotificationAsync_FromUserNotFound_ThrowsInvalidOperationException()
        {
            var notification = CreateTestNotification(fromId: 999); // Non-existent user
             _mockUserService.Setup(s => s.GetUser(notification.FromId)).ReturnsAsync((User)null);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _notificationService.InsertNotificationAsync(notification));
        }


        [TestMethod]
        public async Task InsertNotificationAsync_TokenNeededUserToNull_GeneratesTokenForFromUser()
        {
            var notification = CreateTestNotification(fromId:1, recipientId:0, tokenType: "InviteToken", message: "Invite: {{Verification_Code}}");
            notification.ToEmail = "external@example.com";
            notification.ToName = "External User";

            var fromUser = new User { Id = 1, FirstName = "From", LastName = "User", Email = "from@user.com", Username = "fromUser" };
            var expectedToken = "fromUserToken.secret";
            var expectedVerificationId = "fromUserToken";
            var expectedNewNotificationId = 789;

            _mockUserService.Setup(s => s.GetUser(notification.FromId)).ReturnsAsync(fromUser);
            _mockUserService.Setup(s => s.GetUser(notification.RecipientId)).ReturnsAsync((User)null);
            _mockVerifyTokenService.Setup(s => s.GenerateTokenAsync(fromUser.Id, notification.TokenType)).ReturnsAsync(expectedToken);
            _mockNotificationRepository.Setup(repo => repo.InsertAsync(It.IsAny<Notification>())).ReturnsAsync(expectedNewNotificationId);
            
            var result = await _notificationService.InsertNotificationAsync(notification);

            Assert.AreEqual(expectedNewNotificationId, result.NotificationId);
            Assert.AreEqual(expectedVerificationId, result.VerificationId);
            _mockVerifyTokenService.Verify(s => s.GenerateTokenAsync(fromUser.Id, notification.TokenType), Times.Once);
        }


        #endregion

        #region ProcessPendingNotificationsAsync Tests
        [TestMethod]
        public async Task ProcessPendingNotificationsAsync_SendsEmailAndUpdatesNotification()
        {
            var pendingEmailNotification = CreateTestNotification(id: 10, channel: "email", isSent: false, fromId: 1, toEmail: "test@example.com", toName: "Test User", subject: "Pending", message: "Pending Message");
            var fromUser = new User { Id = 1, Username = "sender" };

            _mockNotificationRepository.Setup(repo => repo.GetNotSentAsync(It.IsAny<DateTime>()))
                                       .ReturnsAsync(new List<Notification> { pendingEmailNotification });
            _mockUserService.Setup(s => s.GetUser(pendingEmailNotification.FromId)).ReturnsAsync(fromUser);
            _mockEmailService.Setup(s => s.SendEmailAsync(pendingEmailNotification.ToEmail, pendingEmailNotification.ToName, fromUser.Username, pendingEmailNotification.Subject, pendingEmailNotification.Message))
                             .Returns(Task.CompletedTask);
            _mockNotificationRepository.Setup(repo => repo.UpdateAsync(It.Is<Notification>(n => n.Id == pendingEmailNotification.Id && n.IsSent)))
                                       .Returns(Task.CompletedTask);

            await _notificationService.ProcessPendingNotificationsAsync();

            _mockEmailService.Verify(s => s.SendEmailAsync(pendingEmailNotification.ToEmail, pendingEmailNotification.ToName, fromUser.Username, pendingEmailNotification.Subject, pendingEmailNotification.Message), Times.Once);
            _mockNotificationRepository.Verify(repo => repo.UpdateAsync(It.Is<Notification>(n => n.Id == pendingEmailNotification.Id && n.IsSent)), Times.Once);
        }

        [TestMethod]
        public async Task ProcessPendingNotificationsAsync_NoPendingNotifications_LogsAndExits()
        {
            _mockNotificationRepository.Setup(repo => repo.GetNotSentAsync(It.IsAny<DateTime>()))
                                       .ReturnsAsync(new List<Notification>());

            await _notificationService.ProcessPendingNotificationsAsync();

            _mockEmailService.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockNotificationRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Notification>()), Times.Never);
             _mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No pending notifications to process.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessPendingNotificationsAsync_UnsupportedChannel_LogsWarningAndSkips()
        {
            var pendingOtherNotification = CreateTestNotification(id:11, channel: "sms", isSent: false);
             _mockNotificationRepository.Setup(repo => repo.GetNotSentAsync(It.IsAny<DateTime>()))
                                       .ReturnsAsync(new List<Notification> { pendingOtherNotification });

            await _notificationService.ProcessPendingNotificationsAsync();

            _mockEmailService.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockNotificationRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Notification>()), Times.Never);
             _mockLogger.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unsupported notification channel 'sms'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessPendingNotificationsAsync_EmailSendFails_LogsErrorAndDoesNotUpdate()
        {
            var pendingEmailNotification = CreateTestNotification(id: 12, channel: "email", isSent: false, fromId:1);
            var fromUser = new User { Id = 1, Username = "sender" };
             _mockNotificationRepository.Setup(repo => repo.GetNotSentAsync(It.IsAny<DateTime>()))
                                       .ReturnsAsync(new List<Notification> { pendingEmailNotification });
            _mockUserService.Setup(s => s.GetUser(pendingEmailNotification.FromId)).ReturnsAsync(fromUser);
            _mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ThrowsAsync(new Exception("SMTP failure"));

            await _notificationService.ProcessPendingNotificationsAsync();

            _mockNotificationRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Notification>()), Times.Never);
             _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error processing email notification ID: 12")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }


        #endregion
    }
}

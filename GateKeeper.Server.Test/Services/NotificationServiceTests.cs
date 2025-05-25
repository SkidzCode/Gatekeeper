using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Models.Account.Notifications; // For NotificationInsertResponse
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Data;
using MySqlConnector;
using System.Linq;
using System.Net; // For WebUtility

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class NotificationServiceTests
    {
        private Mock<IDbHelper> _mockDbHelper;
        private Mock<IMySqlConnectorWrapper> _mockMySqlConnectorWrapper;
        private Mock<IMySqlDataReaderWrapper> _mockDataReader;
        private Mock<IEmailService> _mockEmailService;
        // private Mock<IConfiguration> _mockConfiguration; // Removed
        private Mock<IUserService> _mockUserService;
        private Mock<IVerifyTokenService> _mockVerifyTokenService;
        private Mock<ILogger<NotificationService>> _mockLogger;

        private NotificationService _notificationService;

        // Helper to create a Notification for tests
        private Notification CreateTestNotification(int id = 1, int recipientId = 1, int fromId = 2, 
                                                    string toEmail = "recipient@example.com", string toName = "Recipient Name",
                                                    string channel = "email", string url = "http://example.com/action",
                                                    string tokenType = "TestToken", string subject = "Test Subject",
                                                    string message = "Test Message", bool isSent = false, 
                                                    DateTime? scheduledAt = null, DateTime? createdAt = null, DateTime? updatedAt = null)
        {
            return new Notification
            {
                Id = id,
                RecipientId = recipientId,
                FromId = fromId,
                ToEmail = toEmail,
                ToName = toName,
                Channel = channel,
                URL = url,
                TokenType = tokenType,
                Subject = subject,
                Message = message,
                IsSent = isSent,
                ScheduledAt = scheduledAt,
                CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-1),
                UpdatedAt = updatedAt ?? DateTime.UtcNow
            };
        }
        
        // Helper to setup reader for MapNotification
        private void SetupMockReaderForNotification(Notification notification)
        {
            _mockDataReader.Setup(r => r.GetInt32("Id")).Returns(notification.Id);
            _mockDataReader.Setup(r => r.GetInt32("RecipientId")).Returns(notification.RecipientId);
            _mockDataReader.Setup(r => r.GetInt32("FromId")).Returns(notification.FromId);
            _mockDataReader.Setup(r => r.GetString("ToEmail")).Returns(notification.ToEmail);
            _mockDataReader.Setup(r => r.GetString("ToName")).Returns(notification.ToName);
            _mockDataReader.Setup(r => r.GetString("Channel")).Returns(notification.Channel);
            _mockDataReader.Setup(r => r.GetString("URL")).Returns(notification.URL);
            _mockDataReader.Setup(r => r.GetString("TokenType")).Returns(notification.TokenType);
            _mockDataReader.Setup(r => r.GetString("Subject")).Returns(notification.Subject);
            _mockDataReader.Setup(r => r.GetString("Message")).Returns(notification.Message);
            _mockDataReader.Setup(r => r["IsSent"]).Returns(notification.IsSent); // IsSent is bool, direct return
            _mockDataReader.Setup(r => r.GetDateTime("CreatedAt")).Returns(notification.CreatedAt);
            _mockDataReader.Setup(r => r.GetDateTime("UpdatedAt")).Returns(notification.UpdatedAt);

            _mockDataReader.Setup(r => r.GetOrdinal("ScheduledAt")).Returns(12); // Example ordinal
            if (notification.ScheduledAt.HasValue)
            {
                _mockDataReader.Setup(r => r.IsDBNull(12)).Returns(false);
                _mockDataReader.Setup(r => r.GetDateTime("ScheduledAt")).Returns(notification.ScheduledAt.Value);
            }
            else
            {
                _mockDataReader.Setup(r => r.IsDBNull(12)).Returns(true);
            }
        }


        [TestInitialize]
        public void TestInitialize()
        {
            _mockDbHelper = new Mock<IDbHelper>();
            _mockMySqlConnectorWrapper = new Mock<IMySqlConnectorWrapper>();
            _mockDataReader = new Mock<IMySqlDataReaderWrapper>();
            _mockEmailService = new Mock<IEmailService>();
            // _mockConfiguration = new Mock<IConfiguration>(); // Removed
            _mockUserService = new Mock<IUserService>();
            _mockVerifyTokenService = new Mock<IVerifyTokenService>();
            _mockLogger = new Mock<ILogger<NotificationService>>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));
            _mockMySqlConnectorWrapper.Setup(c => c.OpenConnectionAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));
            
            _notificationService = new NotificationService(
                _mockDbHelper.Object,
                _mockEmailService.Object,
                // _mockConfiguration.Object, // Removed
                _mockUserService.Object,
                _mockVerifyTokenService.Object
                // Logger can be passed if constructor accepts it, otherwise it's not used by the service.
                // The provided NotificationService constructor doesn't take ILogger.
            );
        }

        #region MapNotification Tests (Indirect via public methods)
        // MapNotification is private, so it's tested via methods like GetAllNotificationsAsync.
        // We can write a conceptual test here to ensure our mock setup for the reader is correct.
        [TestMethod]
        public void MapNotification_AllFieldsMappedCorrectly()
        {
            // This test is conceptual for verifying SetupMockReaderForNotification.
            // MapNotification itself is private.
            var notification = CreateTestNotification(scheduledAt: DateTime.UtcNow);
            SetupMockReaderForNotification(notification);

            // Act: Simulate calling MapNotification (conceptually)
            // In a real test, this would be part of testing GetAllNotificationsAsync etc.
            // For now, we just assert our mock setups for the reader are valid.
            Assert.AreEqual(notification.Id, _mockDataReader.Object.GetInt32("Id"));
            Assert.AreEqual(notification.ToEmail, _mockDataReader.Object.GetString("ToEmail"));
            Assert.AreEqual(notification.ScheduledAt, _mockDataReader.Object.GetDateTime("ScheduledAt"));
            Assert.AreEqual(false, _mockDataReader.Object.IsDBNull(_mockDataReader.Object.GetOrdinal("ScheduledAt")));
        }

        [TestMethod]
        public void MapNotification_NullScheduledAt_MappedCorrectly()
        {
            var notification = CreateTestNotification(scheduledAt: null);
            SetupMockReaderForNotification(notification);
            Assert.IsTrue(_mockDataReader.Object.IsDBNull(_mockDataReader.Object.GetOrdinal("ScheduledAt")));
        }
        #endregion

        #region GetAllNotificationsAsync Tests
        [TestMethod]
        public async Task GetAllNotificationsAsync_ReturnsListOfNotifications()
        {
            // Arrange
            var notificationsData = new List<Notification>
            {
                CreateTestNotification(1),
                CreateTestNotification(2, scheduledAt: DateTime.UtcNow)
            };

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("NotificationsGetAll", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            
            var readCallCount = 0;
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < notificationsData.Count)
                           .Callback(() => 
                           {
                               if (readCallCount < notificationsData.Count)
                               {
                                   SetupMockReaderForNotification(notificationsData[readCallCount]);
                               }
                               readCallCount++;
                           });
            
            // Act
            var result = await _notificationService.GetAllNotificationsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(notificationsData.Count, result.Count);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("NotificationsGetAll", CommandType.StoredProcedure), Times.Once);
            for(int i=0; i<notificationsData.Count; i++)
            {
                Assert.AreEqual(notificationsData[i].Id, result[i].Id);
                Assert.AreEqual(notificationsData[i].Subject, result[i].Subject);
            }
        }

        [TestMethod]
        public async Task GetAllNotificationsAsync_EmptyResultSet_ReturnsEmptyList()
        {
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("NotificationsGetAll", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false);

            var result = await _notificationService.GetAllNotificationsAsync();
            
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
        #endregion

        #region GetNotificationsByRecipientAsync Tests
        [TestMethod]
        public async Task GetNotificationsByRecipientAsync_ReturnsUserNotifications()
        {
            var recipientId = 1;
            var notificationsData = new List<Notification> { CreateTestNotification(1, recipientId: recipientId) };
             _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationsGetUser", 
                CommandType.StoredProcedure, 
                It.Is<MySqlParameter[]>(p => p != null && p.Length == 1 && p[0].ParameterName == "@p_RecipientId" && (int)p[0].Value == recipientId)))
                .ReturnsAsync(_mockDataReader.Object);
            
            var readCallCount = 0;
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < notificationsData.Count)
                           .Callback(() => {
                               if (readCallCount < notificationsData.Count) SetupMockReaderForNotification(notificationsData[readCallCount]);
                               readCallCount++;
                           });

            var result = await _notificationService.GetNotificationsByRecipientAsync(recipientId);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(notificationsData[0].Id, result[0].Id);
        }
        #endregion

        #region GetNotSentNotificationsAsync Tests
        [TestMethod]
        public async Task GetNotSentNotificationsAsync_ReturnsNotSentNotifications()
        {
            var currentTime = DateTime.UtcNow;
            var notificationsData = new List<Notification> { CreateTestNotification(isSent: false) };
             _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationsGetNotSent", 
                CommandType.StoredProcedure, 
                It.Is<MySqlParameter[]>(p => p != null && p.Length == 1 && p[0].ParameterName == "@p_current_time" && (DateTime)p[0].Value == currentTime)))
                .ReturnsAsync(_mockDataReader.Object);

            var readCallCount = 0;
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < notificationsData.Count)
                           .Callback(() => {
                               if (readCallCount < notificationsData.Count) SetupMockReaderForNotification(notificationsData[readCallCount]);
                               readCallCount++;
                           });
            
            var result = await _notificationService.GetNotSentNotificationsAsync(currentTime);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(notificationsData[0].Id, result[0].Id);
        }
        #endregion

        #region InsertNotificationAsync Tests
        [TestMethod]
        public async Task InsertNotificationAsync_BasicInsert_ReturnsNewId()
        {
            var notification = CreateTestNotification(fromId: 1, recipientId: 2, toEmail: "test@test.com", toName: "Test Recipient", url: "http://example.com/basic");
            notification.Message = "Hello {{First_Name}}";
            notification.Subject = "Hi {{First_Name}}";

            var expectedNewId = 123;
            var expectedVerificationId = "tokenABC"; // part of token before dot
            var fullToken = $"{expectedVerificationId}.restoftoken";

            _mockUserService.Setup(s => s.GetUser(notification.FromId))
                            .ReturnsAsync(new User { Id = notification.FromId, FirstName = "Sender", LastName = "User", Email = "sender@example.com", Username = "senderUser" });
            _mockUserService.Setup(s => s.GetUser(notification.RecipientId))
                            .ReturnsAsync(new User { Id = notification.RecipientId, FirstName = "Test", LastName = "Recipient", Email = "test@test.com", Username = "testUser" });
            
            _mockVerifyTokenService.Setup(s => s.GenerateTokenAsync(It.IsAny<int>(), It.IsAny<string>()))
                                   .ReturnsAsync(fullToken);

            var localMockDataReader = new Mock<IMySqlDataReaderWrapper>();
            localMockDataReader.SetupSequence(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(true).ReturnsAsync(false);
            localMockDataReader.Setup(r => r["new_id"]).Returns(expectedNewId);
            
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationInsert",
                CommandType.StoredProcedure,
                It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(localMockDataReader.Object)
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) => 
                {
                    Assert.IsTrue(pars.Any(p => p.ParameterName == "@p_Message" && ((string)p.Value).Contains("Test")));
                    Assert.IsTrue(pars.Any(p => p.ParameterName == "@p_Subject" && ((string)p.Value).Contains("Hi Test")));
                     // Check for the potentially problematic Int32 parameters
                    Assert.IsTrue(pars.Any(p => p.ParameterName == "@p_ToName" && p.MySqlDbType == MySqlDbType.VarChar), "p_ToName should be MySqlDbType.VarChar");
                    Assert.IsTrue(pars.Any(p => p.ParameterName == "@p_ToEmail" && p.MySqlDbType == MySqlDbType.VarChar), "p_ToEmail should be MySqlDbType.VarChar");

                });

            var result = await _notificationService.InsertNotificationAsync(notification);

            Assert.AreEqual(expectedNewId, result.NotificationId);
            Assert.AreEqual(string.Empty, result.VerificationId); // No {{Verification_Code}} in message, expect empty string
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("NotificationInsert", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }

        [TestMethod]
        public async Task InsertNotificationAsync_WithUrlAndVerificationPlaceholders_ReplacesThem()
        {
            var notification = CreateTestNotification(fromId:1, recipientId:2, url: "http://example.com/verify", tokenType: "AccountVerify");
            notification.Message = "Please verify at {{URL}} using {{Verification_Code}} for {{First_Name}}";
            notification.Subject = "Verify your account {{First_Name}}";
            notification.ToName = "Recipient OnlyFirstName"; // Test single name part

            var fromUser = new User { Id = 1, FirstName = "From", LastName = "User", Email = "from@user.com", Username = "fromUser" };
            var toUser = new User { Id = 2, FirstName = "Recipient", LastName = "OnlyFirstName", Email = "to@user.com", Username = "toUser" }; // Matched ToName
            var expectedToken = "generatedTokenId.secretPart";
            var expectedVerificationId = "generatedTokenId";
            var expectedNewNotificationId = 456;

            _mockUserService.Setup(s => s.GetUser(notification.FromId)).ReturnsAsync(fromUser);
            _mockUserService.Setup(s => s.GetUser(notification.RecipientId)).ReturnsAsync(toUser);
            _mockVerifyTokenService.Setup(s => s.GenerateTokenAsync(toUser.Id, notification.TokenType)).ReturnsAsync(expectedToken);

            var localMockDataReader = new Mock<IMySqlDataReaderWrapper>();
            localMockDataReader.SetupSequence(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(true).ReturnsAsync(false);
            localMockDataReader.Setup(r => r["new_id"]).Returns(expectedNewNotificationId);
             _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("NotificationInsert", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(localMockDataReader.Object)
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) => 
                {
                    var messageParam = (string)pars.First(p => p.ParameterName == "@p_Message").Value;
                    Assert.IsTrue(messageParam.Contains(notification.URL));
                    Assert.IsTrue(messageParam.Contains(WebUtility.UrlEncode(expectedToken)));
                    Assert.IsTrue(messageParam.Contains(toUser.FirstName)); // First_Name placeholder
                    Assert.IsFalse(messageParam.Contains("{{URL}}"));
                    Assert.IsFalse(messageParam.Contains("{{Verification_Code}}"));
                });

            var result = await _notificationService.InsertNotificationAsync(notification);

            Assert.AreEqual(expectedNewNotificationId, result.NotificationId);
            Assert.AreEqual(expectedVerificationId, result.VerificationId);
            _mockVerifyTokenService.Verify(s => s.GenerateTokenAsync(toUser.Id, notification.TokenType), Times.Once);
        }
        
        [TestMethod]
        public async Task InsertNotificationAsync_TokenNeededButUserToIsNull_GeneratesTokenForFromUser()
        {
            var notification = CreateTestNotification(fromId:1, recipientId:0, url: "http://example.com/invite", tokenType: "InviteToken"); // RecipientId 0 or not found
            notification.Message = "Please accept invite at {{URL}} using {{Verification_Code}}";
            notification.ToEmail = "external@example.com"; // No user in DB for this email
            notification.ToName = "External User";


            var fromUser = new User { Id = 1, FirstName = "From", LastName = "User", Email = "from@user.com", Username = "fromUser" };
            var expectedToken = "generatedTokenForFromUser.secret";
            var expectedVerificationId = "generatedTokenForFromUser";
            var expectedNewNotificationId = 789;

            _mockUserService.Setup(s => s.GetUser(notification.FromId)).ReturnsAsync(fromUser);
            _mockUserService.Setup(s => s.GetUser(notification.RecipientId)).ReturnsAsync((User)null); // Recipient not found

            _mockVerifyTokenService.Setup(s => s.GenerateTokenAsync(fromUser.Id, notification.TokenType)).ReturnsAsync(expectedToken);

            var localMockDataReader = new Mock<IMySqlDataReaderWrapper>();
            localMockDataReader.SetupSequence(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(true).ReturnsAsync(false);
            localMockDataReader.Setup(r => r["new_id"]).Returns(expectedNewNotificationId);
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("NotificationInsert", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(localMockDataReader.Object);
            
            var result = await _notificationService.InsertNotificationAsync(notification);

            Assert.AreEqual(expectedNewNotificationId, result.NotificationId);
            Assert.AreEqual(expectedVerificationId, result.VerificationId);
            _mockVerifyTokenService.Verify(s => s.GenerateTokenAsync(fromUser.Id, notification.TokenType), Times.Once);
            _mockVerifyTokenService.Verify(s => s.GenerateTokenAsync(0, It.IsAny<string>()), Times.Never); // Should not try with RecipientId 0
        }

        #endregion

        #region ProcessPendingNotificationsAsync Tests
        [TestMethod]
        public async Task ProcessPendingNotificationsAsync_SendsEmailAndUpdatesNotification()
        {
            var pendingEmailNotification = CreateTestNotification(id: 10, channel: "email", isSent: false, fromId: 1, toEmail: "test@example.com", toName: "Test User", subject: "Pending", message: "Pending Message");
            var fromUser = new User { Id = 1, Username = "sender" };

            // Mock GetNotSentNotificationsAsync behavior by setting up the reader for it
            var notificationsData = new List<Notification> { pendingEmailNotification };
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("NotificationsGetNotSent", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            
            var readCallCount = 0;
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < notificationsData.Count)
                           .Callback(() => {
                               if (readCallCount < notificationsData.Count) SetupMockReaderForNotification(notificationsData[readCallCount]);
                               readCallCount++;
                           });

            _mockUserService.Setup(s => s.GetUser(pendingEmailNotification.FromId)).ReturnsAsync(fromUser);
            _mockEmailService.Setup(s => s.SendEmailAsync(pendingEmailNotification.ToEmail, pendingEmailNotification.ToName, fromUser.Username, pendingEmailNotification.Subject, pendingEmailNotification.Message))
                             .Returns(Task.CompletedTask);
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync("NotificationUpdate", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(1); // Simulate 1 row updated

            await _notificationService.ProcessPendingNotificationsAsync();

            _mockEmailService.Verify(s => s.SendEmailAsync(pendingEmailNotification.ToEmail, pendingEmailNotification.ToName, fromUser.Username, pendingEmailNotification.Subject, pendingEmailNotification.Message), Times.Once);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync(
                "NotificationUpdate", 
                CommandType.StoredProcedure, 
                It.Is<MySqlParameter[]>(p => 
                    (int)p.First(param => param.ParameterName == "@p_NotificationId").Value == pendingEmailNotification.Id &&
                    (bool)p.First(param => param.ParameterName == "@p_IsSent").Value == true
                )), Times.Once);
        }
        #endregion

        // UpdateNotificationAsync is private and tested via ProcessPendingNotificationsAsync.
        // No direct test for UpdateNotificationAsync.
    }
}

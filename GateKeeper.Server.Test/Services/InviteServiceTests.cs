using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Account.Notifications; // Added for NotificationInsertResponse
using GateKeeper.Server.Models.Site;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Data;
using MySqlConnector; // Required for MySqlParameter

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class InviteServiceTests
    {
        private Mock<IDbHelper> _mockDbHelper;
        private Mock<IMySqlConnectorWrapper> _mockMySqlConnectorWrapper;
        private Mock<ILogger<InviteService>> _mockLogger;
        private Mock<IVerifyTokenService> _mockVerifyTokenService;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<INotificationTemplateService> _mockNotificationTemplateService;

        private InviteService _inviteService;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockDbHelper = new Mock<IDbHelper>();
            _mockMySqlConnectorWrapper = new Mock<IMySqlConnectorWrapper>();
            _mockLogger = new Mock<ILogger<InviteService>>();
            _mockVerifyTokenService = new Mock<IVerifyTokenService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockNotificationTemplateService = new Mock<INotificationTemplateService>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync())
                         .ReturnsAsync(_mockMySqlConnectorWrapper.Object);

            _inviteService = new InviteService(
                _mockDbHelper.Object,
                _mockLogger.Object,
                _mockVerifyTokenService.Object,
                _mockNotificationService.Object,
                _mockNotificationTemplateService.Object
            );
        }

        #region SendInvite Tests

        [TestMethod]
        public async Task SendInvite_SuccessfulInvite_ReturnsNewInviteId()
        {
            // Arrange
            var inviteRequest = new Invite
            {
                ToEmail = "test@example.com",
                ToName = "Test User",
                FromId = 1,
                Website = "http://localhost:5000/accept-invite"
            };
            var expectedInviteId = 123;
            var notificationId = 42;
            var verificationId = "verification-guid";

            var template = new NotificationTemplate
            {
                TemplateName = "Invite Someone", // Corrected template name
                Subject = "You're Invited!",
                Body = "Hello {{ToName}}, please join.",
                TokenType = "InviteToken"
            };

            _mockNotificationTemplateService
                .Setup(s => s.GetNotificationTemplateByNameAsync("Invite Someone", null)) // Corrected template name, added null for languageCode
                .ReturnsAsync(template);

            _mockNotificationService
                .Setup(s => s.InsertNotificationAsync(It.IsAny<Notification>()))
                .ReturnsAsync(new NotificationInsertResponse { NotificationId = notificationId, VerificationId = verificationId })
                .Callback<Notification>(n =>
                {
                    Assert.AreEqual("Email", n.Channel);
                    Assert.AreEqual(template.Subject, n.Subject);
                    Assert.AreEqual(template.Body, n.Message);
                    Assert.AreEqual(0, n.RecipientId); // RecipientId is 0 for invites as per InviteService logic
                    Assert.AreEqual(template.TokenType, n.TokenType);
                    Assert.AreEqual(inviteRequest.Website, n.URL);
                    Assert.AreEqual(inviteRequest.FromId, n.FromId);
                    Assert.AreEqual(inviteRequest.ToEmail, n.ToEmail);
                    Assert.AreEqual(inviteRequest.ToName, n.ToName);
                });
            
            var outputParams = new Dictionary<string, object> { { "@last_id", expectedInviteId } };
            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteNonQueryWithOutputAsync(
                    "InsertInvite",
                    CommandType.StoredProcedure,
                    It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(outputParams)
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) =>
                {
                    Assert.AreEqual(inviteRequest.FromId, pars[0].Value);
                    Assert.AreEqual(inviteRequest.ToName, pars[1].Value);
                    Assert.AreEqual(inviteRequest.ToEmail, pars[2].Value);
                    Assert.AreEqual(verificationId, pars[3].Value); // VerificationId from NotificationService
                    Assert.AreEqual(notificationId, pars[4].Value); // NotificationId from NotificationService
                });

            // Act
            var result = await _inviteService.SendInvite(inviteRequest);

            // Assert
            Assert.AreEqual(expectedInviteId, result);
            _mockNotificationTemplateService.Verify(s => s.GetNotificationTemplateByNameAsync("Invite Someone"), Times.Once);
            _mockNotificationService.Verify(s => s.InsertNotificationAsync(It.IsAny<Notification>()), Times.Once);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryWithOutputAsync("InsertInvite", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }

        [TestMethod]
        public async Task SendInvite_TemplateNotFound_ReturnsZero()
        {
            // Arrange
            var inviteRequest = new Invite
            {
                ToEmail = "test@example.com",
                ToName = "Test User",
                FromId = 1,
                Website = "http://localhost:5000/accept-invite"
            };

            _mockNotificationTemplateService
                .Setup(s => s.GetNotificationTemplateByNameAsync("Invite Someone", null))
                .ReturnsAsync((NotificationTemplate)null);

            // Act
            var result = await _inviteService.SendInvite(inviteRequest);

            // Assert
            Assert.AreEqual(0, result);
            _mockNotificationTemplateService.Verify(s => s.GetNotificationTemplateByNameAsync("Invite Someone", null), Times.Once);
            _mockNotificationService.Verify(s => s.InsertNotificationAsync(It.IsAny<Notification>()), Times.Never);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryWithOutputAsync(It.IsAny<string>(), It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Never);
        }

        #endregion

        #region InsertInvite Tests

        [TestMethod]
        public async Task InsertInvite_CallsDbHelperAndReturnsId()
        {
            // Arrange
            var invite = new Invite
            {
                FromId = 10,
                ToName = "New User",
                ToEmail = "newuser@example.com",
                VerificationId = "verify-guid-123",
                NotificationId = 20
            };
            var expectedInviteId = 456;
            
            var outputParams = new Dictionary<string, object> { { "@last_id", expectedInviteId } };
            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteNonQueryWithOutputAsync(
                    "InsertInvite",
                    CommandType.StoredProcedure,
                    It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(outputParams)
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) =>
                {
                    Assert.AreEqual("InsertInvite", proc);
                    Assert.AreEqual(CommandType.StoredProcedure, type);
                    Assert.AreEqual(invite.FromId, pars[0].Value);
                    Assert.AreEqual(invite.ToName, pars[1].Value);
                    Assert.AreEqual(invite.ToEmail, pars[2].Value);
                    Assert.AreEqual(invite.VerificationId, pars[3].Value);
                    Assert.AreEqual(invite.NotificationId, pars[4].Value);
                });

            // Act
            var result = await _inviteService.InsertInvite(invite);

            // Assert
            Assert.AreEqual(expectedInviteId, result);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryWithOutputAsync("InsertInvite", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }
        
        [TestMethod]
        public async Task InsertInvite_HandlesDbNullNotificationId()
        {
            // Arrange
            var invite = new Invite
            {
                FromId = 11,
                ToName = "Another User",
                ToEmail = "another@example.com",
                VerificationId = "verify-guid-456",
                NotificationId = null // Test DBNull case
            };
            var expectedInviteId = 789;

            var outputParams = new Dictionary<string, object> { { "@last_id", expectedInviteId } };
            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteNonQueryWithOutputAsync(
                    "InsertInvite",
                    CommandType.StoredProcedure,
                    It.Is<MySqlParameter[]>(p => (int)p[0].Value == invite.FromId && (DBNull)p[4].Value == DBNull.Value))) // Check specific parameters
                .ReturnsAsync(outputParams);

            // Act
            var result = await _inviteService.InsertInvite(invite);

            // Assert
            Assert.AreEqual(expectedInviteId, result);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryWithOutputAsync(
                "InsertInvite", 
                CommandType.StoredProcedure, 
                It.Is<MySqlParameter[]>(pars => 
                    pars.Length == 6 && // Ensure all parameters are present
                    (int)pars[0].Value == invite.FromId &&
                    (string)pars[1].Value == invite.ToName &&
                    (string)pars[2].Value == invite.ToEmail &&
                    (string)pars[3].Value == invite.VerificationId &&
                    pars[4].Value == DBNull.Value // Crucial check for NotificationId
                )), Times.Once);
        }


        #endregion

        #region GetInvitesByFromId Tests

        [TestMethod]
        public async Task GetInvitesByFromId_InvitesFound_ReturnsInviteList()
        {
            // Arrange
            var fromId = 1;
            var mockReader = new Mock<IMySqlDataReaderWrapper>();
            var expectedInvites = new List<Invite>
            {
                new Invite { Id = 1, FromId = fromId, ToEmail = "user1@example.com", ToName = "User One", Created = DateTime.UtcNow.AddDays(-1), IsExpired = false, IsRevoked = false, IsComplete = false, IsSent = true },
                new Invite { Id = 2, FromId = fromId, ToEmail = "user2@example.com", ToName = "User Two", Created = DateTime.UtcNow.AddDays(-2), IsExpired = true, IsRevoked = false, IsComplete = false, IsSent = true }
            };

            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteReaderAsync(
                    "GetInvitesByFromId",
                    CommandType.StoredProcedure,
                    It.Is<MySqlParameter[]>(p => (int)p[0].Value == fromId)))
                .ReturnsAsync(mockReader.Object);

            var readCallCount = 0;
            mockReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                      .ReturnsAsync(() => readCallCount < expectedInvites.Count)
                      .Callback(() => readCallCount++);
            
            mockReader.Setup(r => r["Id"]).Returns(() => expectedInvites[readCallCount-1].Id);
            mockReader.Setup(r => r["FromId"]).Returns(() => expectedInvites[readCallCount-1].FromId);
            mockReader.Setup(r => r["ToName"]).Returns(() => expectedInvites[readCallCount-1].ToName);
            mockReader.Setup(r => r["ToEmail"]).Returns(() => expectedInvites[readCallCount-1].ToEmail);
            mockReader.Setup(r => r["Created"]).Returns(() => expectedInvites[readCallCount-1].Created);
            mockReader.Setup(r => r["IsExpired"]).Returns(() => expectedInvites[readCallCount-1].IsExpired);
            mockReader.Setup(r => r["IsRevoked"]).Returns(() => expectedInvites[readCallCount-1].IsRevoked);
            mockReader.Setup(r => r["IsComplete"]).Returns(() => expectedInvites[readCallCount-1].IsComplete);
            mockReader.Setup(r => r["IsSent"]).Returns(() => expectedInvites[readCallCount-1].IsSent);
            
            // Handle DBNull for Created just in case, though test data provides it
            mockReader.Setup(r => r.IsDBNull(It.Is<int>(ordinal => ordinal == mockReader.Object.GetOrdinal("Created"))))
                      .Returns(false);


            // Act
            var result = await _inviteService.GetInvitesByFromId(fromId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedInvites.Count, result.Count);
            for (int i = 0; i < expectedInvites.Count; i++)
            {
                Assert.AreEqual(expectedInvites[i].Id, result[i].Id);
                Assert.AreEqual(expectedInvites[i].ToEmail, result[i].ToEmail);
                Assert.AreEqual(expectedInvites[i].ToName, result[i].ToName);
                Assert.AreEqual(expectedInvites[i].Created.Date, result[i].Created.Date); // Compare date part if needed, or ensure exact match
                Assert.AreEqual(expectedInvites[i].IsExpired, result[i].IsExpired);
                Assert.AreEqual(expectedInvites[i].IsRevoked, result[i].IsRevoked);
                Assert.AreEqual(expectedInvites[i].IsComplete, result[i].IsComplete);
                Assert.AreEqual(expectedInvites[i].IsSent, result[i].IsSent);
            }
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("GetInvitesByFromId", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (int)p[0].Value == fromId)), Times.Once);
        }

        [TestMethod]
        public async Task GetInvitesByFromId_NoInvitesFound_ReturnsEmptyList()
        {
            // Arrange
            var fromId = 1;
            var mockReader = new Mock<IMySqlDataReaderWrapper>();

            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteReaderAsync(
                    "GetInvitesByFromId",
                    CommandType.StoredProcedure,
                    It.Is<MySqlParameter[]>(p => (int)p[0].Value == fromId)))
                .ReturnsAsync(mockReader.Object);

            mockReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                      .ReturnsAsync(false); // Simulate no records

            // Act
            var result = await _inviteService.GetInvitesByFromId(fromId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("GetInvitesByFromId", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (int)p[0].Value == fromId)), Times.Once);
        }

        #endregion
    }
}

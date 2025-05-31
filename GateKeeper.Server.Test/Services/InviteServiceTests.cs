using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Account.Notifications;
using GateKeeper.Server.Models.Site;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Data;
using MySqlConnector;
using System.Threading; // Added for CancellationToken

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
                TemplateName = "InviteUserTemplate",
                Subject = "You're Invited!",
                Body = "Hello {{ToName}}, please join.",
                TokenType = "InviteToken"
            };

            _mockNotificationTemplateService
                .Setup(s => s.GetNotificationTemplateByNameAsync("InviteUserTemplate", null)) // Setup with two arguments
                .ReturnsAsync(template);

            _mockNotificationService
                .Setup(s => s.InsertNotificationAsync(It.IsAny<Notification>()))
                .ReturnsAsync(new NotificationInsertResponse { NotificationId = notificationId, VerificationId = verificationId })
                .Callback<Notification>(n =>
                {
                    Assert.AreEqual("Email", n.Channel);
                    Assert.AreEqual(template.Subject, n.Subject);
                    Assert.AreEqual(template.Body, n.Message);
                    Assert.AreEqual(0, n.RecipientId);
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
                    // Assuming parameters are: FromId, ToName, ToEmail, VerificationId, NotificationId, CreatedDate (auto)
                    // Adjust indices if your SP has different parameter order or count
                    Assert.AreEqual(inviteRequest.FromId, pars[0].Value);
                    Assert.AreEqual(inviteRequest.ToName, pars[1].Value);
                    Assert.AreEqual(inviteRequest.ToEmail, pars[2].Value);
                    Assert.AreEqual(verificationId, pars[3].Value);
                    Assert.AreEqual(notificationId, pars[4].Value);
                });

            // Act
            var result = await _inviteService.SendInvite(inviteRequest);

            // Assert
            Assert.AreEqual(expectedInviteId, result);
            // Corrected Verify to match the Setup call with two arguments
            _mockNotificationTemplateService.Verify(s => s.GetNotificationTemplateByNameAsync("InviteUserTemplate", null), Times.Once);
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
                .Setup(s => s.GetNotificationTemplateByNameAsync("InviteUserTemplate", null))
                .ReturnsAsync((NotificationTemplate)null);

            // Act
            var result = await _inviteService.SendInvite(inviteRequest);

            // Assert
            Assert.AreEqual(0, result);
            // This Verify call correctly matches the Setup (with two arguments)
            _mockNotificationTemplateService.Verify(s => s.GetNotificationTemplateByNameAsync("InviteUserTemplate", null), Times.Once);
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
                    // Parameter order and count for InsertInvite SP:
                    // @p_FromId, @p_ToName, @p_ToEmail, @p_VerificationId, @p_NotificationId, @p_CreatedDate (output @last_id)
                    Assert.AreEqual(invite.FromId, pars.First(p => p.ParameterName == "@p_FromId").Value);
                    Assert.AreEqual(invite.ToName, pars.First(p => p.ParameterName == "@p_ToName").Value);
                    Assert.AreEqual(invite.ToEmail, pars.First(p => p.ParameterName == "@p_ToEmail").Value);
                    Assert.AreEqual(invite.VerificationId, pars.First(p => p.ParameterName == "@p_VerificationId").Value);
                    Assert.AreEqual(invite.NotificationId, pars.First(p => p.ParameterName == "@p_NotificationId").Value);
                    // CreatedDate is usually set by DB, but if passed, it would be pars[5]
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
                    It.Is<MySqlParameter[]>(pars =>
                        pars.Any(p => p.ParameterName == "@p_NotificationId" && p.Value == DBNull.Value) &&
                        pars.Any(p => p.ParameterName == "@p_FromId" && (int)p.Value == invite.FromId)
                    // Add other parameter checks if needed for robustness
                    )))
                .ReturnsAsync(outputParams);

            // Act
            var result = await _inviteService.InsertInvite(invite);

            // Assert
            Assert.AreEqual(expectedInviteId, result);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryWithOutputAsync(
                "InsertInvite",
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(pars =>
                    pars.Length >= 5 && // Check based on your SP's actual parameter count
                    pars.Any(p => p.ParameterName == "@p_FromId" && (int)p.Value == invite.FromId) &&
                    pars.Any(p => p.ParameterName == "@p_ToName" && (string)p.Value == invite.ToName) &&
                    pars.Any(p => p.ParameterName == "@p_ToEmail" && (string)p.Value == invite.ToEmail) &&
                    pars.Any(p => p.ParameterName == "@p_VerificationId" && (string)p.Value == invite.VerificationId) &&
                    pars.Any(p => p.ParameterName == "@p_NotificationId" && p.Value == DBNull.Value) // Crucial check for NotificationId
                )), Times.Once);
        }
        #endregion

        #region GetInvitesByFromId Tests
        // Place this within your InviteServiceTests class
        // IMPORTANT: Verify and adjust ordinal values (0, 1, 2, etc.) to match your SP's column order!
        private void SetupMockReaderForInviteData(Mock<IMySqlDataReaderWrapper> mockReader, Invite invite)
        {
            // Define ordinals (MUST MATCH YOUR SP COLUMN ORDER)
            int idOrdinal = 0;
            int fromIdOrdinal = 1;
            int toNameOrdinal = 2;
            int toEmailOrdinal = 3;
            int verificationIdOrdinal = 4;
            int notificationIdOrdinal = 5;
            int createdOrdinal = 6;
            int isExpiredOrdinal = 7;
            int isRevokedOrdinal = 8;
            int isCompleteOrdinal = 9;
            int isSentOrdinal = 10;
            int acceptedAtOrdinal = 11;
            int acceptedByOrdinal = 12;

            // 1. Mock GetOrdinal for all expected columns
            mockReader.Setup(r => r.GetOrdinal("Id")).Returns(idOrdinal);
            mockReader.Setup(r => r.GetOrdinal("FromId")).Returns(fromIdOrdinal);
            mockReader.Setup(r => r.GetOrdinal("ToName")).Returns(toNameOrdinal);
            mockReader.Setup(r => r.GetOrdinal("ToEmail")).Returns(toEmailOrdinal);
            mockReader.Setup(r => r.GetOrdinal("VerificationId")).Returns(verificationIdOrdinal);
            mockReader.Setup(r => r.GetOrdinal("NotificationId")).Returns(notificationIdOrdinal);
            mockReader.Setup(r => r.GetOrdinal("Created")).Returns(createdOrdinal);
            mockReader.Setup(r => r.GetOrdinal("IsExpired")).Returns(isExpiredOrdinal);
            mockReader.Setup(r => r.GetOrdinal("IsRevoked")).Returns(isRevokedOrdinal);
            mockReader.Setup(r => r.GetOrdinal("IsComplete")).Returns(isCompleteOrdinal);
            mockReader.Setup(r => r.GetOrdinal("IsSent")).Returns(isSentOrdinal);
            mockReader.Setup(r => r.GetOrdinal("AcceptedAt")).Returns(acceptedAtOrdinal);
            mockReader.Setup(r => r.GetOrdinal("AcceptedBy")).Returns(acceptedByOrdinal);

            // 2. Mock the indexer r[columnName]
            mockReader.Setup(r => r["Id"]).Returns(invite.Id);
            mockReader.Setup(r => r["FromId"]).Returns(invite.FromId);
            mockReader.Setup(r => r["ToName"]).Returns(invite.ToName);
            mockReader.Setup(r => r["ToEmail"]).Returns(invite.ToEmail);
            mockReader.Setup(r => r["VerificationId"]).Returns(invite.VerificationId);
            mockReader.Setup(r => r["Created"]).Returns(invite.Created);
            mockReader.Setup(r => r["IsExpired"]).Returns(invite.IsExpired); // Or 1/0 if your DB stores booleans as int
            mockReader.Setup(r => r["IsRevoked"]).Returns(invite.IsRevoked);
            mockReader.Setup(r => r["IsComplete"]).Returns(invite.IsComplete);
            mockReader.Setup(r => r["IsSent"]).Returns(invite.IsSent);

            // Handle nullable fields for the indexer
            mockReader.Setup(r => r["NotificationId"])
                      .Returns(invite.NotificationId.HasValue ? (object)invite.NotificationId.Value : DBNull.Value);
            

            // 3. Mock IsDBNull(ordinal) for nullable columns
            mockReader.Setup(r => r.IsDBNull(notificationIdOrdinal)).Returns(!invite.NotificationId.HasValue);
            
            // For non-nullable columns, IsDBNull should return false (or not be called if SUT checks HasRows first)
            mockReader.Setup(r => r.IsDBNull(idOrdinal)).Returns(false);
            mockReader.Setup(r => r.IsDBNull(fromIdOrdinal)).Returns(false);
            mockReader.Setup(r => r.IsDBNull(toNameOrdinal)).Returns(invite.ToName == null); // If ToName can be null
            mockReader.Setup(r => r.IsDBNull(toEmailOrdinal)).Returns(invite.ToEmail == null); // If ToEmail can be null
            mockReader.Setup(r => r.IsDBNull(verificationIdOrdinal)).Returns(invite.VerificationId == null);
            mockReader.Setup(r => r.IsDBNull(createdOrdinal)).Returns(false); // DateTime usually not null from DB unless nullable type
            mockReader.Setup(r => r.IsDBNull(isExpiredOrdinal)).Returns(false); // Booleans are not null
            mockReader.Setup(r => r.IsDBNull(isRevokedOrdinal)).Returns(false);
            mockReader.Setup(r => r.IsDBNull(isCompleteOrdinal)).Returns(false);
            mockReader.Setup(r => r.IsDBNull(isSentOrdinal)).Returns(false);


            // 4. Mock specific typed getters (good practice, SUT might use them)
            mockReader.Setup(r => r.GetInt32("Id")).Returns(invite.Id);
            mockReader.Setup(r => r.GetInt32("FromId")).Returns(invite.FromId);
            mockReader.Setup(r => r.GetString("ToName")).Returns(invite.ToName);
            mockReader.Setup(r => r.GetString("ToEmail")).Returns(invite.ToEmail);
            mockReader.Setup(r => r.GetString("VerificationId")).Returns(invite.VerificationId);

            if (invite.NotificationId.HasValue)
            {
                mockReader.Setup(r => r.GetInt32("NotificationId")).Returns(invite.NotificationId.Value);
            }
            mockReader.Setup(r => r.GetDateTime("Created")).Returns(invite.Created);
            // Assuming booleans are read as integers 0 or 1 from DB if GetInt32 is used, or directly as bool if GetBoolean
            mockReader.Setup(r => r.GetInt32("IsExpired")).Returns(invite.IsExpired ? 1 : 0);
            // mockReader.Setup(r => r.GetBoolean("IsExpired")).Returns(invite.IsExpired); // If GetBoolean is used
            mockReader.Setup(r => r.GetInt32("IsRevoked")).Returns(invite.IsRevoked ? 1 : 0);
            mockReader.Setup(r => r.GetInt32("IsComplete")).Returns(invite.IsComplete ? 1 : 0);
            mockReader.Setup(r => r.GetInt32("IsSent")).Returns(invite.IsSent ? 1 : 0);

            
        }


        [TestMethod]
        public async Task GetInvitesByFromId_InvitesFound_ReturnsInviteList()
        {
            // Arrange
            var fromId = 1;
            var mockDataReader = new Mock<IMySqlDataReaderWrapper>();
            var expectedInvites = new List<Invite>
    {
        new Invite { Id = 1, FromId = fromId, ToEmail = "user1@example.com", ToName = "User One", VerificationId = "v1", NotificationId = 10, Created = DateTime.UtcNow.AddDays(-1), IsExpired = false, IsRevoked = false, IsComplete = false, IsSent = true },
        new Invite { Id = 2, FromId = fromId, ToEmail = "user2@example.com", ToName = "User Two", VerificationId = "v2", NotificationId = 11, Created = DateTime.UtcNow.AddDays(-2), IsExpired = true, IsRevoked = false, IsComplete = false, IsSent = true }
    };

            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteReaderAsync(
                    "GetInvitesByFromId",
                    CommandType.StoredProcedure,
                    It.Is<MySqlParameter[]>(p => p.Length == 1 && (int)p[0].Value == fromId && p[0].ParameterName == "@p_FromId")))
                .ReturnsAsync(mockDataReader.Object);

            var readCallCount = 0;
            // This sequence setup is crucial for simulating multiple rows
            mockDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                          .ReturnsAsync(() => readCallCount < expectedInvites.Count) // Returns true if there's more data
                          .Callback(() =>
                          {
                              if (readCallCount < expectedInvites.Count)
                              {
                                  // Setup the mock reader for the CURRENT row's data
                                  SetupMockReaderForInviteData(mockDataReader, expectedInvites[readCallCount]);
                              }
                              readCallCount++;
                          });

            // Act
            var result = await _inviteService.GetInvitesByFromId(fromId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedInvites.Count, result.Count, "The number of returned invites should match expected.");
            for (int i = 0; i < expectedInvites.Count; i++)
            {
                Assert.AreEqual(expectedInvites[i].Id, result[i].Id, $"Invite {i} Id mismatch.");
                Assert.AreEqual(expectedInvites[i].ToEmail, result[i].ToEmail, $"Invite {i} ToEmail mismatch.");
                Assert.AreEqual(expectedInvites[i].ToName, result[i].ToName, $"Invite {i} ToName mismatch.");
                // Assert.AreEqual(expectedInvites[i].VerificationId, result[i].VerificationId, $"Invite {i} VerificationId mismatch.");
                // Assert.AreEqual(expectedInvites[i].NotificationId, result[i].NotificationId, $"Invite {i} NotificationId mismatch.");
                Assert.AreEqual(expectedInvites[i].Created.Date, result[i].Created.Date, $"Invite {i} Created date mismatch."); // Comparing Date part for simplicity
                Assert.AreEqual(expectedInvites[i].IsExpired, result[i].IsExpired, $"Invite {i} IsExpired mismatch.");
                Assert.AreEqual(expectedInvites[i].IsRevoked, result[i].IsRevoked, $"Invite {i} IsRevoked mismatch.");
                Assert.AreEqual(expectedInvites[i].IsComplete, result[i].IsComplete, $"Invite {i} IsComplete mismatch.");
                Assert.AreEqual(expectedInvites[i].IsSent, result[i].IsSent, $"Invite {i} IsSent mismatch.");
            }
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("GetInvitesByFromId", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (int)p[0].Value == fromId)), Times.Once);
        }

        [TestMethod]
        public async Task GetInvitesByFromId_NoInvitesFound_ReturnsEmptyList()
        {
            // Arrange
            var fromId = 1;
            var mockDataReader = new Mock<IMySqlDataReaderWrapper>();

            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteReaderAsync(
                    "GetInvitesByFromId",
                    CommandType.StoredProcedure,
                    It.Is<MySqlParameter[]>(p => p.Length == 1 && (int)p[0].Value == fromId && p[0].ParameterName == "@p_FromId")))
                .ReturnsAsync(mockDataReader.Object);

            mockDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                          .ReturnsAsync(false); // Simulate no records

            // Act
            var result = await _inviteService.GetInvitesByFromId(fromId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        #endregion
    }
}
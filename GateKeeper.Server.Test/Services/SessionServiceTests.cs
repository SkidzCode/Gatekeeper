using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Account.UserModels; // For User in TokenVerificationResponse
using GateKeeper.Server.Models.Account.Login; // For VerifyTokenRequest, TokenVerificationResponse
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Data;
using MySqlConnector;
using System.Linq;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class SessionServiceTests
    {
        // private Mock<IConfiguration> _mockConfiguration; // Removed
        private Mock<IDbHelper> _mockDbHelper;
        private Mock<IMySqlConnectorWrapper> _mockMySqlConnectorWrapper;
        private Mock<IMySqlDataReaderWrapper> _mockDataReader;
        private Mock<ILogger<SessionService>> _mockLogger;
        private Mock<IVerifyTokenService> _mockVerifyTokenService;
        private SessionService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            // _mockConfiguration = new Mock<IConfiguration>(); // Removed
            _mockDbHelper = new Mock<IDbHelper>();
            _mockMySqlConnectorWrapper = new Mock<IMySqlConnectorWrapper>();
            _mockDataReader = new Mock<IMySqlDataReaderWrapper>();
            _mockLogger = new Mock<ILogger<SessionService>>();
            _mockVerifyTokenService = new Mock<IVerifyTokenService>();

            // Mock IConfiguration GetSection for DatabaseConfig
            // The SUT uses .Get<DatabaseConfig>() ?? new DatabaseConfig(), so GetSection returning a section
            // where Get<T> might return null (Moq default) is acceptable.
            // var mockDbConfigSection = new Mock<IConfigurationSection>(); // No longer needed as IConfiguration is removed
            // _mockConfiguration.Setup(c => c.GetSection("DatabaseConfig")).Returns(mockDbConfigSection.Object); // No longer needed
            
            _mockDbHelper.Setup(db => db.GetWrapperAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));
            _mockMySqlConnectorWrapper.Setup(c => c.OpenConnectionAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));

            _service = new SessionService(
                /* _mockConfiguration.Object, */ // Removed
                _mockDbHelper.Object,
                _mockLogger.Object,
                _mockVerifyTokenService.Object
            );
        }

        private SessionModel CreateTestSessionModel(string id = "session123", int userId = 1, string verificationId = "verifyABC",
                                                    DateTime? expiry = null, bool complete = false, bool revoked = false,
                                                    string ipAddress = "127.0.0.1", string userAgent = "TestAgent", string sessionData = "{}",
                                                    string verifyType = "Refresh", DateTime? verificationExpiry = null,
                                                    bool? verificationComplete = false, bool? verificationRevoked = false)
        {
            return new SessionModel
            {
                Id = id,
                UserId = userId,
                VerificationId = verificationId,
                ExpiryDate = expiry ?? DateTime.UtcNow.AddHours(1),
                Complete = complete,
                Revoked = revoked,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-10),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                SessionData = sessionData,
                VerifyType = verifyType,
                VerificationExpiryDate = verificationExpiry ?? DateTime.UtcNow.AddHours(1),
                VerificationComplete = verificationComplete,
                VerificationRevoked = verificationRevoked
            };
        }
        
        private void SetupMockReaderForSession(SessionModel session, bool includeVerificationFields = true)
        {
            _mockDataReader.Setup(r => r["Id"]).Returns(session.Id);
            _mockDataReader.Setup(r => r.GetInt32("UserId")).Returns(session.UserId); // Assuming direct mapping
            _mockDataReader.Setup(r => r["VerificationId"]).Returns(session.VerificationId);
            _mockDataReader.Setup(r => r.GetDateTime("ExpiryDate")).Returns(session.ExpiryDate);
            _mockDataReader.Setup(r => r["Complete"]).Returns(session.Complete);
            _mockDataReader.Setup(r => r["Revoked"]).Returns(session.Revoked);
            _mockDataReader.Setup(r => r["CreatedAt"]).Returns(session.CreatedAt);
            _mockDataReader.Setup(r => r["UpdatedAt"]).Returns(session.UpdatedAt);
            _mockDataReader.Setup(r => r["IpAddress"]).Returns(session.IpAddress);
            _mockDataReader.Setup(r => r["UserAgent"]).Returns(session.UserAgent);
            _mockDataReader.Setup(r => r["SessionData"]).Returns(session.SessionData);

            if (includeVerificationFields)
            {
                _mockDataReader.Setup(r => r["VerifyType"]).Returns(session.VerifyType);
                _mockDataReader.Setup(r => r["VerificationExpiryDate"]).Returns(session.VerificationExpiryDate);
                _mockDataReader.Setup(r => r["VerificationComplete"]).Returns(session.VerificationComplete);
                _mockDataReader.Setup(r => r["VerificationRevoked"]).Returns(session.VerificationRevoked);
            } else { // For GetMostRecentActivity which reads fewer verification fields
                 _mockDataReader.Setup(r => r["VerifyType"]).Returns(session.VerifyType);
                _mockDataReader.Setup(r => r["VerificationExpiryDate"]).Returns(session.VerificationExpiryDate);
            }
        }

        #region InsertSession Tests
        [TestMethod]
        public async Task InsertSession_CallsSPWithCorrectParameters()
        {
            var session = CreateTestSessionModel();

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync(
                "SessionInsert",
                CommandType.StoredProcedure,
                It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(1) // Rows affected
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) =>
                {
                    Assert.AreEqual("SessionInsert", proc);
                    Assert.AreEqual(session.Id, pars.First(p => p.ParameterName == "@pId").Value);
                    Assert.AreEqual(session.UserId, pars.First(p => p.ParameterName == "@pUserId").Value);
                    // ... other parameters
                });

            await _service.InsertSession(session);

            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync("SessionInsert", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }
        #endregion

        #region RefreshSession Tests
        [TestMethod]
        public async Task RefreshSession_CallsSPAndReturnsNewSessionId()
        {
            var userId = 1;
            var oldVerificationId = "oldVerify123";
            var newVerificationId = "newVerify456";
            var expectedNewSessionId = "newSessionXYZ";

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync(
                "SessionRefresh",
                CommandType.StoredProcedure,
                It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(1)
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) =>
                {
                    Assert.AreEqual("SessionRefresh", proc);
                    Assert.AreEqual(userId, pars.First(p => p.ParameterName == "@pUserId").Value);
                    Assert.AreEqual(oldVerificationId, pars.First(p => p.ParameterName == "@pOldVerificationId").Value);
                    Assert.AreEqual(newVerificationId, pars.First(p => p.ParameterName == "@pNewVerificationId").Value);
                    
                    // Simulate output parameter being set by DB
                    var outputParam = pars.First(p => p.ParameterName == "@pSessionId" && p.Direction == ParameterDirection.Output);
                    outputParam.Value = expectedNewSessionId;
                });

            var result = await _service.RefreshSession(userId, oldVerificationId, newVerificationId);

            Assert.AreEqual(expectedNewSessionId, result);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync("SessionRefresh", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }
        #endregion

        #region LogoutToken Tests
        [TestMethod]
        public async Task LogoutToken_ValidTokenAndUser_CallsSP()
        {
            var token = "verifyId123.secretPart";
            var userId = 1;
            var verificationId = "verifyId123";

            _mockVerifyTokenService.Setup(vts => vts.VerifyTokenAsync(It.Is<VerifyTokenRequest>(r => r.VerificationCode == token && r.TokenType == "Refresh")))
                .ReturnsAsync(new TokenVerificationResponse { IsVerified = true, User = new User { Id = userId } });

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync(
                "SessionLogout", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (string)p[0].Value == verificationId)))
                .ReturnsAsync(1);

            await _service.LogoutToken(token, userId);

            _mockVerifyTokenService.Verify(vts => vts.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()), Times.Once);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync("SessionLogout", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (string)p[0].Value == verificationId)), Times.Once);
        }

        [TestMethod]
        public async Task LogoutToken_VerificationFails_DoesNotCallSP()
        {
            var token = "invalidToken.secret";
            var userId = 1;

            _mockVerifyTokenService.Setup(vts => vts.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()))
                .ReturnsAsync(new TokenVerificationResponse { IsVerified = false });
            
            await _service.LogoutToken(token, userId);

            _mockVerifyTokenService.Verify(vts => vts.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()), Times.Once);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync("SessionLogout", It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Never);
        }
        
        [TestMethod]
        public async Task LogoutToken_UserMismatch_DoesNotCallSP()
        {
            var token = "verifyId123.secretPart";
            var userId = 1; // User performing logout
            var tokenOwnerUserId = 2; // Actual owner of the token

            _mockVerifyTokenService.Setup(vts => vts.VerifyTokenAsync(It.Is<VerifyTokenRequest>(r => r.VerificationCode == token && r.TokenType == "Refresh")))
                .ReturnsAsync(new TokenVerificationResponse { IsVerified = true, User = new User { Id = tokenOwnerUserId } });
            
            await _service.LogoutToken(token, userId);

            _mockVerifyTokenService.Verify(vts => vts.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()), Times.Once);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync("SessionLogout", It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Never);
        }
        #endregion

        #region LogoutSession Tests
        [TestMethod]
        public async Task LogoutSession_CallsSPWithCorrectParameters()
        {
            var sessionId = "sessionToLogout123"; // This is the Session.Id (VerificationId in SUT parameter name)
            var userId = 1; // userId parameter in SUT LogoutSession is not used by the SP call

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync(
                "SessionIdLogout", // SUT uses "SessionIdLogout" SP
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (string)p[0].Value == sessionId && p[0].ParameterName == "@pSessionId")))
                .ReturnsAsync(1);

            await _service.LogoutSession(sessionId, userId);

            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync("SessionIdLogout", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (string)p[0].Value == sessionId)), Times.Once);
        }
        #endregion

        #region GetActiveSessionsForUser Tests
        [TestMethod]
        public async Task GetActiveSessionsForUser_SessionsFound_ReturnsSessionList()
        {
            var userId = 1;
            var sessionsData = new List<SessionModel> { CreateTestSessionModel(userId: userId, id: "s1"), CreateTestSessionModel(userId: userId, id: "s2") };

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("SessionActiveListForUser", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (int)p[0].Value == userId)))
                                      .ReturnsAsync(_mockDataReader.Object);
            
            var readCallCount = 0;
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < sessionsData.Count)
                           .Callback(() => {
                               if (readCallCount < sessionsData.Count) SetupMockReaderForSession(sessionsData[readCallCount]);
                               readCallCount++;
                           });
            
            var result = await _service.GetActiveSessionsForUser(userId);

            Assert.IsNotNull(result);
            Assert.AreEqual(sessionsData.Count, result.Count);
            Assert.AreEqual(sessionsData[0].Id, result[0].Id);
        }

        [TestMethod]
        public async Task GetActiveSessionsForUser_NoSessions_ReturnsEmptyList()
        {
            var userId = 1;
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("SessionActiveListForUser", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false);
            
            var result = await _service.GetActiveSessionsForUser(userId);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
        #endregion

        #region GetMostRecentActivity Tests
        [TestMethod]
        public async Task GetMostRecentActivity_ActivityFound_ReturnsSessionList()
        {
            // SUT returns List<SessionModel>, not a single SessionModel
            var sessionsData = new List<SessionModel> { CreateTestSessionModel(id: "recent1") }; 

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("SessionListMostRecentActivity", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            
            var readCallCount = 0;
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < sessionsData.Count)
                           .Callback(() => {
                               // For GetMostRecentActivity, SUT doesn't read all verification fields
                               if (readCallCount < sessionsData.Count) SetupMockReaderForSession(sessionsData[readCallCount], false);
                               readCallCount++;
                           });
            
            var result = await _service.GetMostRecentActivity();

            Assert.IsNotNull(result);
            Assert.AreEqual(sessionsData.Count, result.Count);
            Assert.AreEqual(sessionsData[0].Id, result[0].Id);
        }

        [TestMethod]
        public async Task GetMostRecentActivity_NoActivity_ReturnsEmptyList()
        {
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("SessionListMostRecentActivity", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false);
            
            var result = await _service.GetMostRecentActivity();

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count); // SUT returns empty list, not null
        }
        #endregion
    }
}

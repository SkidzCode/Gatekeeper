using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Models.Account.Login;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class SessionServiceTests
    {
        private Mock<ISessionRepository> _mockSessionRepository;
        private Mock<ILogger<SessionService>> _mockLogger;
        private Mock<IVerifyTokenService> _mockVerifyTokenService;
        private SessionService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockSessionRepository = new Mock<ISessionRepository>();
            _mockLogger = new Mock<ILogger<SessionService>>();
            _mockVerifyTokenService = new Mock<IVerifyTokenService>();

            _service = new SessionService(
                _mockSessionRepository.Object,
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
                Id = id, UserId = userId, VerificationId = verificationId, ExpiryDate = expiry ?? DateTime.UtcNow.AddHours(1),
                Complete = complete, Revoked = revoked, CreatedAt = DateTime.UtcNow.AddMinutes(-30), UpdatedAt = DateTime.UtcNow.AddMinutes(-10),
                IpAddress = ipAddress, UserAgent = userAgent, SessionData = sessionData, VerifyType = verifyType,
                VerificationExpiryDate = verificationExpiry ?? DateTime.UtcNow.AddHours(1),
                VerificationComplete = verificationComplete, VerificationRevoked = verificationRevoked
            };
        }

        #region InsertSession Tests
        [TestMethod]
        public async Task InsertSession_CallsRepositoryInsertAsync()
        {
            var session = CreateTestSessionModel();
            _mockSessionRepository.Setup(repo => repo.InsertAsync(session)).Returns(Task.CompletedTask);

            await _service.InsertSession(session);

            _mockSessionRepository.Verify(repo => repo.InsertAsync(session), Times.Once);
        }
        #endregion

        #region RefreshSession Tests
        [TestMethod]
        public async Task RefreshSession_CallsRepositoryRefreshAsync_ReturnsNewSessionId()
        {
            var userId = 1;
            var oldVerificationId = "oldVerify123";
            var newVerificationId = "newVerify456";
            var expectedNewSessionId = "newSessionXYZ";
            _mockSessionRepository.Setup(repo => repo.RefreshAsync(userId, oldVerificationId, newVerificationId))
                                  .ReturnsAsync(expectedNewSessionId);

            var result = await _service.RefreshSession(userId, oldVerificationId, newVerificationId);

            Assert.AreEqual(expectedNewSessionId, result);
            _mockSessionRepository.Verify(repo => repo.RefreshAsync(userId, oldVerificationId, newVerificationId), Times.Once);
        }
        #endregion

        #region LogoutToken Tests
        [TestMethod]
        public async Task LogoutToken_ValidTokenAndUser_CallsRepositoryLogoutByVerificationId()
        {
            var token = "verifyId123.secretPart";
            var userId = 1;
            var verificationId = "verifyId123";
            _mockVerifyTokenService.Setup(vts => vts.VerifyTokenAsync(It.Is<VerifyTokenRequest>(r => r.VerificationCode == token && r.TokenType == "Refresh")))
                .ReturnsAsync(new TokenVerificationResponse { IsVerified = true, User = new User { Id = userId } });
            _mockSessionRepository.Setup(repo => repo.LogoutByVerificationIdAsync(verificationId)).Returns(Task.CompletedTask);

            await _service.LogoutToken(token, userId);

            _mockSessionRepository.Verify(repo => repo.LogoutByVerificationIdAsync(verificationId), Times.Once);
        }

        [TestMethod]
        public async Task LogoutToken_VerificationFails_DoesNotCallRepository()
        {
            var token = "invalidToken.secret";
            var userId = 1;
            _mockVerifyTokenService.Setup(vts => vts.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()))
                .ReturnsAsync(new TokenVerificationResponse { IsVerified = false });

            await _service.LogoutToken(token, userId);

            _mockSessionRepository.Verify(repo => repo.LogoutByVerificationIdAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task LogoutToken_UserMismatch_DoesNotCallRepository()
        {
            var token = "verifyId123.secretPart";
            var userId = 1;
            var tokenOwnerUserId = 2;
            _mockVerifyTokenService.Setup(vts => vts.VerifyTokenAsync(It.Is<VerifyTokenRequest>(r => r.VerificationCode == token && r.TokenType == "Refresh")))
                .ReturnsAsync(new TokenVerificationResponse { IsVerified = true, User = new User { Id = tokenOwnerUserId } });

            await _service.LogoutToken(token, userId);

            _mockSessionRepository.Verify(repo => repo.LogoutByVerificationIdAsync(It.IsAny<string>()), Times.Never);
        }
        #endregion

        #region LogoutSession Tests
        [TestMethod]
        public async Task LogoutSession_CallsRepositoryLogoutBySessionId()
        {
            var sessionId = "sessionToLogout123";
            var userId = 1;
            _mockSessionRepository.Setup(repo => repo.LogoutBySessionIdAsync(sessionId)).Returns(Task.CompletedTask);

            await _service.LogoutSession(sessionId, userId);

            _mockSessionRepository.Verify(repo => repo.LogoutBySessionIdAsync(sessionId), Times.Once);
        }
        #endregion

        #region GetActiveSessionsForUser Tests
        [TestMethod]
        public async Task GetActiveSessionsForUser_SessionsFound_ReturnsSessionList()
        {
            var userId = 1;
            var sessionsData = new List<SessionModel> { CreateTestSessionModel(userId: userId, id: "s1") };
            _mockSessionRepository.Setup(repo => repo.GetActiveByUserIdAsync(userId)).ReturnsAsync(sessionsData);

            var result = await _service.GetActiveSessionsForUser(userId);

            Assert.IsNotNull(result);
            Assert.AreEqual(sessionsData.Count, result.Count);
            _mockSessionRepository.Verify(repo => repo.GetActiveByUserIdAsync(userId), Times.Once);
        }
        #endregion

        #region GetMostRecentActivity Tests
        [TestMethod]
        public async Task GetMostRecentActivity_ActivityFound_ReturnsSessionList()
        {
            var sessionsData = new List<SessionModel> { CreateTestSessionModel(id: "recent1") };
            _mockSessionRepository.Setup(repo => repo.GetMostRecentAsync()).ReturnsAsync(sessionsData);

            var result = await _service.GetMostRecentActivity();

            Assert.IsNotNull(result);
            Assert.AreEqual(sessionsData.Count, result.Count);
            _mockSessionRepository.Verify(repo => repo.GetMostRecentAsync(), Times.Once);
        }
        #endregion
    }
}

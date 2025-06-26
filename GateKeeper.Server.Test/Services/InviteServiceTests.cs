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
using System.Linq;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class InviteServiceTests
    {
        private Mock<IInviteRepository> _mockInviteRepository;
        private Mock<ILogger<InviteService>> _mockLogger;
        private Mock<IVerifyTokenService> _mockVerifyTokenService;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<INotificationTemplateService> _mockNotificationTemplateService;

        private InviteService _inviteService;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockInviteRepository = new Mock<IInviteRepository>();
            _mockLogger = new Mock<ILogger<InviteService>>();
            _mockVerifyTokenService = new Mock<IVerifyTokenService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockNotificationTemplateService = new Mock<INotificationTemplateService>();

            _inviteService = new InviteService(
                _mockInviteRepository.Object,
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
                .Setup(s => s.GetNotificationTemplateByNameAsync("InviteUserTemplate", null))
                .ReturnsAsync(template);

            _mockNotificationService
                .Setup(s => s.InsertNotificationAsync(It.IsAny<Notification>()))
                .ReturnsAsync(new NotificationInsertResponse { NotificationId = notificationId, VerificationId = verificationId });

            _mockInviteRepository
                .Setup(r => r.InsertInviteAsync(It.Is<Invite>(i =>
                    i.ToEmail == inviteRequest.ToEmail &&
                    i.ToName == inviteRequest.ToName &&
                    i.FromId == inviteRequest.FromId &&
                    i.Website == inviteRequest.Website &&
                    i.NotificationId == notificationId && // Check that NotificationId from InsertNotificationAsync is used
                    i.VerificationId == verificationId  // Check that VerificationId from InsertNotificationAsync is used
                )))
                .ReturnsAsync(expectedInviteId);

            // Act
            var result = await _inviteService.SendInvite(inviteRequest);

            // Assert
            Assert.AreEqual(expectedInviteId, result);
            _mockNotificationTemplateService.Verify(s => s.GetNotificationTemplateByNameAsync("InviteUserTemplate", null), Times.Once);
            _mockNotificationService.Verify(s => s.InsertNotificationAsync(It.IsAny<Notification>()), Times.Once);
            _mockInviteRepository.Verify(r => r.InsertInviteAsync(It.IsAny<Invite>()), Times.Once);
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
            _mockNotificationTemplateService.Verify(s => s.GetNotificationTemplateByNameAsync("InviteUserTemplate", null), Times.Once);
            _mockNotificationService.Verify(s => s.InsertNotificationAsync(It.IsAny<Notification>()), Times.Never);
            _mockInviteRepository.Verify(r => r.InsertInviteAsync(It.IsAny<Invite>()), Times.Never);
        }

        #endregion

        #region InsertInvite Tests

        [TestMethod]
        public async Task InsertInvite_CallsRepositoryAndReturnsId()
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

            _mockInviteRepository
                .Setup(r => r.InsertInviteAsync(invite))
                .ReturnsAsync(expectedInviteId);

            // Act
            var result = await _inviteService.InsertInvite(invite);

            // Assert
            Assert.AreEqual(expectedInviteId, result);
            _mockInviteRepository.Verify(r => r.InsertInviteAsync(invite), Times.Once);
        }

        #endregion

        #region GetInvitesByFromId Tests

        [TestMethod]
        public async Task GetInvitesByFromId_InvitesFound_ReturnsInviteList()
        {
            // Arrange
            var fromId = 1;
            var expectedInvites = new List<Invite>
            {
                new Invite { Id = 1, FromId = fromId, ToEmail = "user1@example.com", ToName = "User One", VerificationId = "v1", NotificationId = 10, Created = DateTime.UtcNow.AddDays(-1), IsExpired = false, IsRevoked = false, IsComplete = false, IsSent = true },
                new Invite { Id = 2, FromId = fromId, ToEmail = "user2@example.com", ToName = "User Two", VerificationId = "v2", NotificationId = 11, Created = DateTime.UtcNow.AddDays(-2), IsExpired = true, IsRevoked = false, IsComplete = false, IsSent = true }
            };

            _mockInviteRepository
                .Setup(r => r.GetInvitesByFromIdAsync(fromId))
                .ReturnsAsync(expectedInvites);

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
                Assert.AreEqual(expectedInvites[i].Created.Date, result[i].Created.Date, $"Invite {i} Created date mismatch.");
                Assert.AreEqual(expectedInvites[i].IsExpired, result[i].IsExpired, $"Invite {i} IsExpired mismatch.");
                Assert.AreEqual(expectedInvites[i].IsRevoked, result[i].IsRevoked, $"Invite {i} IsRevoked mismatch.");
                Assert.AreEqual(expectedInvites[i].IsComplete, result[i].IsComplete, $"Invite {i} IsComplete mismatch.");
                Assert.AreEqual(expectedInvites[i].IsSent, result[i].IsSent, $"Invite {i} IsSent mismatch.");
            }
            _mockInviteRepository.Verify(r => r.GetInvitesByFromIdAsync(fromId), Times.Once);
        }

        [TestMethod]
        public async Task GetInvitesByFromId_NoInvitesFound_ReturnsEmptyList()
        {
            // Arrange
            var fromId = 1;
            _mockInviteRepository
                .Setup(r => r.GetInvitesByFromIdAsync(fromId))
                .ReturnsAsync(new List<Invite>());

            // Act
            var result = await _inviteService.GetInvitesByFromId(fromId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            _mockInviteRepository.Verify(r => r.GetInvitesByFromIdAsync(fromId), Times.Once);
        }

        #endregion
    }
}
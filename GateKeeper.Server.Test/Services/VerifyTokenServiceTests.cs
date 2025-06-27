using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Account.Login;
using GateKeeper.Server.Models.Account.UserModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class VerifyTokenServiceTests
    {
        private Mock<IVerifyTokenRepository> _mockVerifyTokenRepository;
        private Mock<ILogger<VerifyTokenService>> _mockLogger;
        private Mock<IUserService> _mockUserService;
        private Mock<IEmailService> _mockEmailService; // Kept for constructor, can be removed if not used
        private VerifyTokenService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockVerifyTokenRepository = new Mock<IVerifyTokenRepository>();
            _mockLogger = new Mock<ILogger<VerifyTokenService>>();
            _mockUserService = new Mock<IUserService>();
            _mockEmailService = new Mock<IEmailService>();

            _service = new VerifyTokenService(
                _mockVerifyTokenRepository.Object,
                _mockLogger.Object,
                _mockUserService.Object,
                _mockEmailService.Object
            );
        }

        // Helper to create VerificationTokenDetails for tests
        private VerificationTokenDetails CreateMockTokenDetails(
            int userId, string verifyType, string tokenSalt, string hashedToken,
            bool isRevoked = false, bool isComplete = false,
            string firstName = "Test", string lastName = "User", string email = "test@example.com",
            string phone = "123", string userSalt = "usersalt", string userPass = "userhash", string username = "testuser")
        {
            return new VerificationTokenDetails
            {
                UserId = userId,
                VerifyType = verifyType,
                RefreshSalt = tokenSalt, // This is the token's salt
                HashedToken = hashedToken,
                Revoked = isRevoked,
                Complete = isComplete,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                UserSalt = userSalt, // User's main password salt
                UserPassword = userPass, // User's main password hash
                Username = username
            };
        }


        #region GenerateTokenAsync Tests
        [TestMethod]
        public async Task GenerateTokenAsync_ReturnsTokenInCorrectFormatAndStoresIt()
        {
            // Arrange
            var userId = 1;
            var tokenType = "PasswordReset";
            var expectedTokenId = Guid.NewGuid().ToString();

            _mockVerifyTokenRepository.Setup(repo => repo.StoreTokenAsync(
                userId,
                tokenType,
                It.IsAny<string>(), // hashedToken
                It.IsAny<string>(), // salt
                It.Is<DateTime>(dt => dt > DateTime.UtcNow && dt < DateTime.UtcNow.AddDays(8)) // expiryDate
            )).ReturnsAsync(expectedTokenId);

            // Act
            var resultToken = await _service.GenerateTokenAsync(userId, tokenType);

            // Assert
            Assert.IsNotNull(resultToken);
            var parts = resultToken.Split('.');
            Assert.AreEqual(2, parts.Length, "Token should be in format 'tokenId.rawTokenValue'");
            Assert.AreEqual(expectedTokenId, parts[0], "TokenId part should match the one returned by repository.");
            Assert.IsFalse(string.IsNullOrEmpty(parts[1]), "Raw token part should not be empty.");

            _mockVerifyTokenRepository.Verify(repo => repo.StoreTokenAsync(
                userId, tokenType, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
        }
        #endregion

        #region VerifyTokenAsync Tests

        [TestMethod]
        public async Task VerifyTokenAsync_ValidToken_ReturnsVerifiedResponse()
        {
            // Arrange
            var userId = 1;
            var tokenType = "PasswordReset";
            var rawTokenValue = "mySecretTokenValue";
            var tokenSalt = PasswordHelper.GenerateSalt();
            var hashedToken = PasswordHelper.HashPassword(rawTokenValue, tokenSalt);
            var tokenId = Guid.NewGuid().ToString(); // This is what SessionId in response becomes
            var request = new VerifyTokenRequest { VerificationCode = $"{tokenId}.{rawTokenValue}", TokenType = tokenType };

            var mockDetails = CreateMockTokenDetails(userId, tokenType, tokenSalt, hashedToken);
            _mockVerifyTokenRepository.Setup(repo => repo.GetTokenDetailsForVerificationAsync(tokenId))
                                      .ReturnsAsync(mockDetails);
            _mockUserService.Setup(us => us.GetRolesAsync(userId)).ReturnsAsync(new List<string> { "User" });
            
            // Act
            var response = await _service.VerifyTokenAsync(request);

            // Assert
            Assert.IsTrue(response.IsVerified);
            Assert.AreEqual(tokenId, response.SessionId); // SessionId in response is the tokenId
            Assert.AreEqual(tokenType, response.TokenType);
            Assert.IsNotNull(response.User);
            Assert.AreEqual(userId, response.User.Id);
            Assert.IsNull(response.FailureReason);
        }

        [TestMethod]
        public async Task VerifyTokenAsync_InvalidTokenFormat_ReturnsNotVerified()
        {
            var request = new VerifyTokenRequest { VerificationCode = "invalidformat", TokenType = "Test" };
            var response = await _service.VerifyTokenAsync(request);
            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Invalid token format", response.FailureReason);
            _mockVerifyTokenRepository.Verify(repo => repo.GetTokenDetailsForVerificationAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task VerifyTokenAsync_TokenIdNotFound_ReturnsNotVerified()
        {
            var tokenId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{tokenId}.token", TokenType = "Test" };
            _mockVerifyTokenRepository.Setup(repo => repo.GetTokenDetailsForVerificationAsync(tokenId))
                                      .ReturnsAsync((VerificationTokenDetails?)null); // Simulate not found

            var response = await _service.VerifyTokenAsync(request);
            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Invalid Session Id", response.FailureReason);
        }

        [TestMethod]
        public async Task VerifyTokenAsync_TokenRevoked_ReturnsNotVerified()
        {
            var tokenId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{tokenId}.token", TokenType = "Test" };
            var mockDetails = CreateMockTokenDetails(1, "Test", "salt", "hashed", isRevoked: true);
            _mockVerifyTokenRepository.Setup(repo => repo.GetTokenDetailsForVerificationAsync(tokenId))
                                      .ReturnsAsync(mockDetails);
            
            var response = await _service.VerifyTokenAsync(request);
            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Token already revoked", response.FailureReason);
        }

        [TestMethod]
        public async Task VerifyTokenAsync_TokenCompleted_ReturnsNotVerified()
        {
            var tokenId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{tokenId}.token", TokenType = "Test" };
             var mockDetails = CreateMockTokenDetails(1, "Test", "salt", "hashed", isComplete: true);
            _mockVerifyTokenRepository.Setup(repo => repo.GetTokenDetailsForVerificationAsync(tokenId))
                                      .ReturnsAsync(mockDetails);
            
            var response = await _service.VerifyTokenAsync(request);
            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Token already completed", response.FailureReason);
        }
        
        [TestMethod]
        public async Task VerifyTokenAsync_TokenTypeMismatch_ReturnsNotVerified()
        {
            var tokenId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{tokenId}.token", TokenType = "WrongType" };
            var mockDetails = CreateMockTokenDetails(1, "CorrectType", "salt", "hashed");
            _mockVerifyTokenRepository.Setup(repo => repo.GetTokenDetailsForVerificationAsync(tokenId))
                                      .ReturnsAsync(mockDetails);
            
            var response = await _service.VerifyTokenAsync(request);
            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Incorrect token type", response.FailureReason);
        }

        [TestMethod]
        public async Task VerifyTokenAsync_TokenHashMismatch_ReturnsNotVerifiedAndClearsPhi()
        {
            var userId = 1;
            var tokenType = "PasswordReset";
            var rawTokenValue = "mySecretTokenValue";
            var tokenSalt = PasswordHelper.GenerateSalt();
            // var correctHashedToken = PasswordHelper.HashPassword(rawTokenValue, tokenSalt);
            var wrongHashedTokenInDb = PasswordHelper.HashPassword("wrongValue", tokenSalt);
            var tokenId = Guid.NewGuid().ToString();
            var request = new VerifyTokenRequest { VerificationCode = $"{tokenId}.{rawTokenValue}", TokenType = tokenType };

            var mockDetails = CreateMockTokenDetails(userId, tokenType, tokenSalt, wrongHashedTokenInDb);
            _mockVerifyTokenRepository.Setup(repo => repo.GetTokenDetailsForVerificationAsync(tokenId))
                                      .ReturnsAsync(mockDetails);
            _mockUserService.Setup(us => us.GetRolesAsync(userId)).ReturnsAsync(new List<string> { "User" });
            
            var response = await _service.VerifyTokenAsync(request);

            Assert.IsFalse(response.IsVerified);
            Assert.AreEqual("Invalid token", response.FailureReason);
            Assert.IsNotNull(response.User, "User object should be populated even on hash mismatch for PHI clearing.");
            Assert.IsTrue(string.IsNullOrEmpty(response.User.FirstName), "PHI should be cleared on hash mismatch.");
            _mockUserService.Verify(us => us.GetRolesAsync(userId), Times.Once);
        }
        #endregion

        #region RevokeTokensAsync Tests
        [TestMethod]
        public async Task RevokeTokensAsync_CallsRepositoryAndReturnsRowsAffected()
        {
            var userId = 1;
            var tokenType = "Session";
            var token = "tokenId.tokenValue";
            var expectedTokenId = "tokenId";
            var expectedRowsAffected = 1;

            _mockVerifyTokenRepository.Setup(repo => repo.RevokeTokensAsync(userId, tokenType, expectedTokenId))
                .ReturnsAsync(expectedRowsAffected);

            var result = await _service.RevokeTokensAsync(userId, tokenType, token);
            Assert.AreEqual(expectedRowsAffected, result);
            _mockVerifyTokenRepository.Verify(repo => repo.RevokeTokensAsync(userId, tokenType, expectedTokenId), Times.Once);
        }
        
        [TestMethod]
        public async Task RevokeTokensAsync_NullToken_CallsRepositoryWithNullTokenId()
        {
            var userId = 1;
            var tokenType = "Session";
            var expectedRowsAffected = 1;

             _mockVerifyTokenRepository.Setup(repo => repo.RevokeTokensAsync(userId, tokenType, null))
                .ReturnsAsync(expectedRowsAffected);

            var result = await _service.RevokeTokensAsync(userId, tokenType, null);
            Assert.AreEqual(expectedRowsAffected, result);
            _mockVerifyTokenRepository.Verify(repo => repo.RevokeTokensAsync(userId, tokenType, null), Times.Once);
        }

        [TestMethod]
        public async Task RevokeTokensAsync_TokenWithoutDot_AssumesIsTokenId()
        {
            var userId = 1;
            var tokenType = "Session";
            var tokenIdOnly = "justTheTokenId";
            var expectedRowsAffected = 1;

            _mockVerifyTokenRepository.Setup(repo => repo.RevokeTokensAsync(userId, tokenType, tokenIdOnly))
                .ReturnsAsync(expectedRowsAffected);

            var result = await _service.RevokeTokensAsync(userId, tokenType, tokenIdOnly);
            Assert.AreEqual(expectedRowsAffected, result);
             _mockVerifyTokenRepository.Verify(repo => repo.RevokeTokensAsync(userId, tokenType, tokenIdOnly), Times.Once);
        }


        #endregion

        #region CompleteTokensAsync Tests
        [TestMethod]
        public async Task CompleteTokensAsync_CallsRepositoryAndReturnsRowsAffected()
        {
            var userId = 1;
            var tokenType = "PasswordReset";
            var token = "tokenId.tokenValue";
            var expectedTokenId = "tokenId";
            var expectedRowsAffected = 1;

            _mockVerifyTokenRepository.Setup(repo => repo.CompleteTokensAsync(userId, tokenType, expectedTokenId))
                .ReturnsAsync(expectedRowsAffected);

            var result = await _service.CompleteTokensAsync(userId, tokenType, token);
            Assert.AreEqual(expectedRowsAffected, result);
            _mockVerifyTokenRepository.Verify(repo => repo.CompleteTokensAsync(userId, tokenType, expectedTokenId), Times.Once);
        }
        #endregion
    }
}

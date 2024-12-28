using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using GateKeeper.Server.Models.Site;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class UserAuthenticationServiceTests
    {
        private Mock<IDBHelper> _mockDbHelper;
        private Mock<ILogger<UserAuthenticationService>> _mockLogger;
        private Mock<IUserService> _mockUserService;
        private Mock<IEmailService> _mockEmailService;
        private Mock<IVerifyTokenService> _mockVerificationService;
        private Mock<ISettingsService> _mockSettingsService;
        private UserAuthenticationService _authService;

        [TestInitialize]
        public void Setup()
        {
            _mockDbHelper = new Mock<IDBHelper>();
            _mockLogger = new Mock<ILogger<UserAuthenticationService>>();
            _mockUserService = new Mock<IUserService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockVerificationService = new Mock<IVerifyTokenService>();
            _mockSettingsService = new Mock<ISettingsService>();
            var mockConfiguration = new Mock<IConfiguration>();

            // Set up the JWT configuration values
            mockConfiguration.SetupGet(c => c["JwtConfig:Secret"]).Returns("your_secret_key");
            mockConfiguration.SetupGet(c => c["JwtConfig:ExpirationMinutes"]).Returns("60");
            mockConfiguration.SetupGet(c => c["JwtConfig:Issuer"]).Returns("your_issuer");
            mockConfiguration.SetupGet(c => c["JwtConfig:Audience"]).Returns("your_audience");

            // Set up the PasswordStrength configuration values
            mockConfiguration.SetupGet(c => c["PasswordStrength:MinLength"]).Returns("8");
            mockConfiguration.SetupGet(c => c["PasswordStrength:RequireUppercase"]).Returns("true");
            mockConfiguration.SetupGet(c => c["PasswordStrength:RequireLowercase"]).Returns("true");
            mockConfiguration.SetupGet(c => c["PasswordStrength:RequireDigit"]).Returns("true");
            mockConfiguration.SetupGet(c => c["PasswordStrength:RequireSpecialChar"]).Returns("true");
            mockConfiguration.SetupGet(c => c["PasswordStrength:SpecialChars"]).Returns("!@#$%^&*()_-+=[{]};:'\",.<>/?`~");

            _authService = new UserAuthenticationService(
                _mockUserService.Object,
                _mockVerificationService.Object,
                mockConfiguration.Object,
                _mockDbHelper.Object,
                _mockLogger.Object,
                _mockEmailService.Object,
                _mockSettingsService.Object
            );
        }

        [TestMethod]
        public async Task RegisterUserAsync_ShouldRegisterUserSuccessfully()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Username = "johndoe",
                Password = "StrongPassword123!",
                Phone = "1234567890",
                Website = "http://example.com"
            };

            _mockUserService.Setup(us => us.AddUser(It.IsAny<User>()))
                .ReturnsAsync((0, new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Username = "johndoe" }));

            _mockVerificationService.Setup(vs => vs.GenerateTokenAsync(It.IsAny<int>(), "NewUser"))
                .ReturnsAsync("verification_token");

            _mockEmailService.Setup(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _authService.RegisterUserAsync(registerRequest);

            // Assert
            _mockUserService.Verify(us => us.AddUser(It.IsAny<User>()), Times.Once);
            _mockVerificationService.Verify(vs => vs.GenerateTokenAsync(It.IsAny<int>(), "NewUser"), Times.Once);
            _mockEmailService.Verify(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_ShouldAuthenticateUserSuccessfully()
        {
            // Arrange
            var userLoginRequest = new UserLoginRequest
            {
                Identifier = "johndoe",
                Password = "password123"
            };

            var user = new User
            {
                Id = 1,
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "uPTjKd7CONhMrjqtEUbtj0IrVYjp2tqokEGPtsqQlCg=",
                Salt = "salt",
                Roles = new List<string> { "User" }
            };

            _mockUserService.Setup(us => us.GetUser(It.IsAny<string>())).ReturnsAsync(user);
            _mockSettingsService.Setup(ss => ss.GetAllSettingsAsync(It.IsAny<int?>())).ReturnsAsync(new List<Setting>());
            _mockVerificationService.Setup(vs => vs.GenerateTokenAsync(It.IsAny<int>(), "Refresh")).ReturnsAsync("refresh_token");

            // Act
            var (isAuthenticated, accessToken, refreshToken, authenticatedUser, settings) = await _authService.LoginAsync(userLoginRequest);

            // Assert
            Assert.IsTrue(isAuthenticated);
            Assert.IsNotNull(accessToken);
            Assert.IsNotNull(refreshToken);
            Assert.IsNotNull(authenticatedUser);
            _mockUserService.Verify(us => us.GetUser(It.IsAny<string>()), Times.Once);
            _mockVerificationService.Verify(vs => vs.GenerateTokenAsync(It.IsAny<int>(), "Refresh"), Times.Once);
        }

        [TestMethod]
        public async Task LogoutAsync_ShouldRevokeTokensSuccessfully()
        {
            // Arrange
            int userId = 1;
            string token = "refresh_token";

            _mockVerificationService.Setup(vs => vs.RevokeTokensAsync(userId, "Refresh", token)).ReturnsAsync(1);

            // Act
            var result = await _authService.LogoutAsync(token, userId);

            // Assert
            Assert.AreEqual(1, result);
            _mockVerificationService.Verify(vs => vs.RevokeTokensAsync(userId, "Refresh", token), Times.Once);
        }

        [TestMethod]
        public async Task VerifyNewUser_ShouldVerifyUserSuccessfully()
        {
            // Arrange
            string verificationCode = "verification_code";
            var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Username = "johndoe"  };

            _mockVerificationService.Setup(vs => vs.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()))
                .ReturnsAsync((true, user, "NewUser"));

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).ReturnsAsync(Mock.Of<IMySqlConnectorWrapper>());

            // Act
            var (isValid, verifiedUser, validationType) = await _authService.VerifyNewUser(verificationCode);

            // Assert
            Assert.IsTrue(isValid);
            Assert.IsNotNull(verifiedUser);
            Assert.AreEqual("NewUser", validationType);
            _mockVerificationService.Verify(vs => vs.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()), Times.Once);
        }

        [TestMethod]
        public async Task RefreshTokensAsync_ShouldRefreshTokensSuccessfully()
        {
            // Arrange
            string refreshToken = "refresh_token";
            var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Username = "johndoe", Roles = [] };

            _mockVerificationService.Setup(vs => vs.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()))
                .ReturnsAsync((true, user, "Refresh"));

            _mockSettingsService.Setup(ss => ss.GetAllSettingsAsync(It.IsAny<int?>())).ReturnsAsync(new List<Setting>());
            _mockVerificationService.Setup(vs => vs.GenerateTokenAsync(It.IsAny<int>(), "Refresh")).ReturnsAsync("new_refresh_token");

            // Act
            var (isSuccessful, newAccessToken, newRefreshToken, refreshedUser, settings) = await _authService.RefreshTokensAsync(refreshToken);

            // Assert
            Assert.IsTrue(isSuccessful);
            Assert.IsNotNull(newAccessToken);
            Assert.IsNotNull(newRefreshToken);
            Assert.IsNotNull(refreshedUser);
            _mockVerificationService.Verify(vs => vs.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()), Times.Once);
            _mockVerificationService.Verify(vs => vs.GenerateTokenAsync(It.IsAny<int>(), "Refresh"), Times.Once);
        }
    }
}

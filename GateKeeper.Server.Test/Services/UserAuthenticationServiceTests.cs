using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Models.Account.Login;
using GateKeeper.Server.Models.Account.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GateKeeper.Server.Models.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using GateKeeper.Server.Models.Site;
using System.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using GateKeeper.Server.Exceptions;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class UserAuthenticationServiceTests
    {
        private Mock<ILogger<UserAuthenticationService>> _mockLogger;
        private Mock<IUserService> _mockUserService;
        private Mock<IVerifyTokenService> _mockVerificationService;
        private Mock<ISettingsService> _mockSettingsService;
        private Mock<IKeyManagementService> _mockKeyManagementService;
        private Mock<IStringDataProtector> _mockStringDataProtector;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<IResponseCookies> _mockResponseCookies;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<INotificationTemplateService> _mockNotificationTemplateService;
        private Mock<ISessionService> _mockSessionService;
        private Mock<IUserAuthenticationRepository> _mockUserAuthRepository;
        private UserAuthenticationService _authService;
        private Mock<IOptions<JwtSettingsConfig>> _mockJwtSettingsOptions;
        private Mock<IOptions<PasswordSettingsConfig>> _mockPasswordSettingsOptions;
        private Mock<IOptions<RegisterSettingsConfig>> _mockRegisterSettingsOptions;
        private Mock<IOptions<LoginSettingsConfig>> _mockLoginSettingsOptions;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<UserAuthenticationService>>();
            _mockUserService = new Mock<IUserService>();
            _mockVerificationService = new Mock<IVerifyTokenService>();
            _mockSettingsService = new Mock<ISettingsService>();
            _mockKeyManagementService = new Mock<IKeyManagementService>();
            _mockStringDataProtector = new Mock<IStringDataProtector>(MockBehavior.Strict);
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockNotificationTemplateService = new Mock<INotificationTemplateService>();
            _mockSessionService = new Mock<ISessionService>();
            _mockUserAuthRepository = new Mock<IUserAuthenticationRepository>();

            var jwtSettings = new JwtSettingsConfig
            {
                Key = "your_super_secret_key_that_is_at_least_32_bytes_long_for_hs256",
                Issuer = "your_issuer",
                Audience = "your_audience",
                TokenValidityInMinutes = 60,
                RefreshTokenValidityInDays = 7
            };
            _mockJwtSettingsOptions = new Mock<IOptions<JwtSettingsConfig>>();
            _mockJwtSettingsOptions.Setup(o => o.Value).Returns(jwtSettings);

            var passwordSettings = new PasswordSettingsConfig
            {
                RequiredLength = 8,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = true,
                MaxFailedAccessAttempts = 5
            };
            _mockPasswordSettingsOptions = new Mock<IOptions<PasswordSettingsConfig>>();
            _mockPasswordSettingsOptions.Setup(o => o.Value).Returns(passwordSettings);

            var registerSettings = new RegisterSettingsConfig
            {
                RequireInvite = false,
                DefaultRole = "User",
                RequireEmailConfirmation = true
            };
            _mockRegisterSettingsOptions = new Mock<IOptions<RegisterSettingsConfig>>();
            _mockRegisterSettingsOptions.Setup(o => o.Value).Returns(registerSettings);

            var loginSettings = new LoginSettingsConfig
            {
                MaxFailedAccessAttempts = 5,
                CookieExpiryMinutes = 15,
                LockoutEnabled = true,
                LockoutDurationInMinutes = 30
            };
            _mockLoginSettingsOptions = new Mock<IOptions<LoginSettingsConfig>>();
            _mockLoginSettingsOptions.Setup(o => o.Value).Returns(loginSettings);
            
            _mockStringDataProtector.Setup(p => p.Protect(It.IsAny<string>())).Returns((string s) => s + "_protected");
            _mockStringDataProtector.Setup(p => p.Unprotect(It.IsAny<string>())).Returns("0");

            var secureString = new SecureString();
            foreach (char c in "yjulQ1tDEQ+8tzqgRSWQ0OCpsp1idl5W+KMq3ROqFEQ=")
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();
            _mockKeyManagementService.Setup(kms => kms.GetCurrentKeyAsync()).ReturnsAsync(secureString);

            var mockHttpContext = new Mock<HttpContext>();
            var mockHttpRequest = new Mock<HttpRequest>();
            var mockHttpResponse = new Mock<HttpResponse>();
            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            _mockResponseCookies = new Mock<IResponseCookies>();

            mockHttpContext.Setup(ctx => ctx.Items).Returns(new Dictionary<object, object?>());
            mockHttpRequest.Setup(req => req.Cookies).Returns(mockRequestCookies.Object);
            mockHttpRequest.Setup(req => req.Scheme).Returns("https");
            mockHttpRequest.Setup(req => req.Host).Returns(new HostString("localhost"));
            mockHttpRequest.Setup(req => req.Path).Returns(new PathString("/"));

            var mockConnectionInfo = new Mock<ConnectionInfo>();
            mockConnectionInfo.Setup(ci => ci.RemoteIpAddress).Returns(System.Net.IPAddress.Loopback);
            mockHttpContext.Setup(ctx => ctx.Connection).Returns(mockConnectionInfo.Object);

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockHttpContext.Setup(ctx => ctx.RequestServices).Returns(mockServiceProvider.Object);

            var features = new Microsoft.AspNetCore.Http.Features.FeatureCollection();
            var mockHttpResponseFeature = new Mock<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>();
            mockHttpResponseFeature.Setup(f => f.Headers).Returns(new HeaderDictionary());
            mockHttpResponseFeature.Setup(f => f.Body).Returns(new System.IO.MemoryStream());
            features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(mockHttpResponseFeature.Object);
            features.Set<Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature>(new Mock<Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature>().Object);
            mockHttpContext.Setup(ctx => ctx.Features).Returns(features);

            mockHttpResponse.Setup(res => res.Cookies).Returns(_mockResponseCookies.Object);
            mockHttpContext.Setup(ctx => ctx.Request).Returns(mockHttpRequest.Object);
            mockHttpContext.Setup(ctx => ctx.Response).Returns(mockHttpResponse.Object);
            _mockHttpContextAccessor.Setup(hca => hca.HttpContext).Returns(mockHttpContext.Object);

            _mockSettingsService.Setup(ss => ss.GetAllSettingsAsync(It.IsAny<int?>())).ReturnsAsync(new List<Setting>());

            ReinitializeAuthService();
        }

        private void ReinitializeAuthService()
        {
            _authService = new UserAuthenticationService(
                _mockUserService.Object,
                _mockVerificationService.Object,
                _mockJwtSettingsOptions.Object,
                _mockPasswordSettingsOptions.Object,
                _mockRegisterSettingsOptions.Object,
                _mockLoginSettingsOptions.Object,
                _mockLogger.Object,
                _mockSettingsService.Object,
                _mockKeyManagementService.Object,
                _mockStringDataProtector.Object,
                _mockHttpContextAccessor.Object,
                _mockNotificationService.Object,
                _mockNotificationTemplateService.Object,
                _mockSessionService.Object,
                _mockUserAuthRepository.Object
            );
        }

        [TestMethod]
        public async Task RegisterUserAsync_ShouldRegisterUserSuccessfully()
        {
            var registerRequest = new RegisterRequest
            {
                FirstName = "John", LastName = "Doe", Email = "john.doe@example.com",
                Username = "johndoe", Password = "StrongPassword123!", Phone = "1234567890", Website = "http://example.com"
            };
            var registrationResponse = new RegistrationResponse { IsSuccessful = true, User = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Username = "johndoe" } };
            _mockUserService.Setup(us => us.RegisterUser(It.IsAny<User>())).ReturnsAsync(registrationResponse);
            _mockUserAuthRepository.Setup(repo => repo.AssignRoleToUserAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _mockNotificationTemplateService.Setup(nts => nts.GetNotificationTemplateByNameAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(new NotificationTemplate { Body = "template_body", Subject = "template_subject" });
            _mockNotificationService.Setup(ns => ns.InsertNotificationAsync(It.IsAny<Notification>())).ReturnsAsync(new NotificationInsertResponse());
            _mockVerificationService.Setup(vs => vs.GenerateTokenAsync(It.IsAny<int>(), "NewUser")).ReturnsAsync("verification_token");
            _mockVerificationService.Setup(vs => vs.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>())).ReturnsAsync(new TokenVerificationResponse { IsVerified = true, User = new User { Id = registerRequest.Token == "valid_invite_token" ? 100 : 0 } });
            _mockVerificationService.Setup(vs => vs.CompleteTokensAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(1);

            await _authService.RegisterUserAsync(registerRequest);

            _mockUserService.Verify(us => us.RegisterUser(It.IsAny<User>()), Times.Once);
            _mockNotificationService.Verify(ns => ns.InsertNotificationAsync(It.IsAny<Notification>()), Times.Once);
            _mockUserAuthRepository.Verify(repo => repo.AssignRoleToUserAsync(registrationResponse.User.Id, "NewUser"), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_ShouldAuthenticateUserSuccessfully()
        {
            var userLoginRequest = new UserLoginRequest { Identifier = "johndoe", Password = "password123" };
            var ipAddress = "127.0.0.1";
            var userAgent = "TestAgent";
            var user = new User { Id = 1, Username = "johndoe", Email = "john.doe@example.com", Password = "uPTjKd7CONhMrjqtEUbtj0IrVYjp2tqokEGPtsqQlCg=", Salt = "salt", Roles = new List<string> { "User" } };
            _mockUserService.Setup(us => us.GetUser(It.IsAny<string>())).ReturnsAsync(user);
            _mockSettingsService.Setup(ss => ss.GetAllSettingsAsync(It.IsAny<int?>())).ReturnsAsync(new List<Setting>());
            _mockVerificationService.Setup(vs => vs.GenerateTokenAsync(It.IsAny<int>(), "Refresh")).ReturnsAsync("refresh_token");
            _mockSessionService.Setup(ss => ss.InsertSession(It.IsAny<SessionModel>())).Returns(Task.CompletedTask);

            var loginResponse = await _authService.LoginAsync(userLoginRequest, ipAddress, userAgent);
            _mockUserService.Verify(us => us.GetUser(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task LogoutAsync_ShouldRevokeTokensSuccessfully()
        {
            int userId = 1;
            string token = "refresh_token.part2";
            _mockVerificationService.Setup(vs => vs.RevokeTokensAsync(userId, "Refresh", token)).ReturnsAsync(1);
            _mockSessionService.Setup(ss => ss.LogoutToken(It.IsAny<string>(), It.IsAny<int>())).Returns(Task.CompletedTask);
            var result = await _authService.LogoutAsync(token, userId);
            Assert.AreEqual(1, result);
            _mockSessionService.Verify(ss => ss.LogoutToken(token, userId), Times.Once);
        }

        [TestMethod]
        public async Task VerifyNewUser_ShouldVerifyUserSuccessfully()
        {
            string verificationCode = "verification_code";
            var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Username = "johndoe" };
            var tokenVerificationResponse = new TokenVerificationResponse { IsVerified = true, User = user, TokenType = "NewUser", SessionId = "session123" };
            _mockVerificationService.Setup(vs => vs.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>())).ReturnsAsync(tokenVerificationResponse);
            _mockUserAuthRepository.Setup(repo => repo.ValidateFinishAsync(user.Id, "session123")).Returns(Task.CompletedTask);
            var result = await _authService.VerifyNewUser(verificationCode);
            Assert.IsTrue(result.IsVerified);
            Assert.IsNotNull(result.User);
            Assert.AreEqual("NewUser", result.TokenType);
            _mockVerificationService.Verify(vs => vs.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()), Times.Once);
            _mockUserAuthRepository.Verify(repo => repo.ValidateFinishAsync(user.Id, "session123"), Times.Once);
        }

        [TestMethod]
        public async Task RefreshTokensAsync_ShouldRefreshTokensSuccessfully()
        {
            string refreshToken = "refresh_token.part2";
            var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Username = "johndoe", Roles = new List<string>() };
            var tokenVerificationResponse = new TokenVerificationResponse { IsVerified = true, User = user, TokenType = "Refresh", SessionId = "session123" };
            _mockVerificationService.Setup(vs => vs.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>())).ReturnsAsync(tokenVerificationResponse);
            _mockVerificationService.Setup(vs => vs.RevokeTokensAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(1);
            _mockSettingsService.Setup(ss => ss.GetAllSettingsAsync(It.IsAny<int?>())).ReturnsAsync(new List<Setting>());
            _mockVerificationService.Setup(vs => vs.GenerateTokenAsync(It.IsAny<int>(), "Refresh")).ReturnsAsync("new_refresh_token");
            _mockSessionService.Setup(ss => ss.RefreshSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("new_session_id");
            var result = await _authService.RefreshTokensAsync(refreshToken);
            _mockVerificationService.Verify(vs => vs.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_UserNotFound_ThrowsUserNotFoundException()
        {
            var userLoginRequest = new UserLoginRequest { Identifier = "unknownuser", Password = "password" };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync((User)null);
            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c[It.IsAny<string>()]).Returns((string)null);
            _mockHttpContextAccessor.Object.HttpContext.Request.Cookies = mockRequestCookies.Object;
            await Assert.ThrowsExceptionAsync<UserNotFoundException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            _mockUserService.Verify(us => us.GetUser(userLoginRequest.Identifier), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_UserSaltNull_ThrowsUserNotFoundException()
        {
            var userLoginRequest = new UserLoginRequest { Identifier = "userwithnullsalt", Password = "password" };
            var userWithNullSalt = new User { Id = 2, Username = "userwithnullsalt", Password = "hashedpassword", Salt = null };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(userWithNullSalt);
            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c[It.IsAny<string>()]).Returns((string)null);
            _mockHttpContextAccessor.Object.HttpContext.Request.Cookies = mockRequestCookies.Object;
            await Assert.ThrowsExceptionAsync<UserNotFoundException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            _mockUserService.Verify(us => us.GetUser(userLoginRequest.Identifier), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_IncorrectPassword_ThrowsInvalidCredentialsException()
        {
            var userLoginRequest = new UserLoginRequest { Identifier = "johndoe", Password = "wrongpassword" };
            var user = new User { Id = 1, Username = "johndoe", Email = "john.doe@example.com", Password = "uPTjKd7CONhMrjqtEUbtj0IrVYjp2tqokEGPtsqQlCg=", Salt = "salt", Roles = new List<string> { "User" } };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(user);
            _mockStringDataProtector.Setup(p => p.Unprotect(It.IsAny<string>())).Returns("0");
            var initialRequestCookies = Mock.Get(_mockHttpContextAccessor.Object.HttpContext.Request.Cookies);
            initialRequestCookies.Setup(c => c["LoginAttempts"]).Returns((string)null);
            ReinitializeAuthService();
            await Assert.ThrowsExceptionAsync<InvalidCredentialsException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            _mockUserService.Verify(us => us.GetUser(userLoginRequest.Identifier), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_AccountLocked_ThrowsAccountLockedException()
        {
            var userLoginRequest = new UserLoginRequest { Identifier = "lockeduser", Password = "password" };
            var user = new User { Id = 3, Username = "lockeduser", Salt = "salt", Password = "hashedpassword" };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(user);
            var loginSettings = new LoginSettingsConfig { MaxFailedAccessAttempts = 1, CookieExpiryMinutes = 15, LockoutEnabled = true, LockoutDurationInMinutes = 30 };
            _mockLoginSettingsOptions.Setup(o => o.Value).Returns(loginSettings);
            var requestCookiesFromSetup = _mockHttpContextAccessor.Object.HttpContext.Request.Cookies;
            var requestCookiesMock = Mock.Get(requestCookiesFromSetup);
            _mockStringDataProtector.Setup(p => p.Unprotect("locked_cookie_value")).Returns("1");
            requestCookiesMock.Setup(c => c["LoginAttempts"]).Returns("locked_cookie_value");
            ReinitializeAuthService();
            await Assert.ThrowsExceptionAsync<AccountLockedException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            _mockUserService.Verify(us => us.GetUser(userLoginRequest.Identifier), Times.Once);
        }

        [TestMethod]
        public async Task RegisterUserAsync_InvalidInviteToken_ThrowsInvalidTokenException()
        {
            var registerRequest = new RegisterRequest { Email = "test@example.com", Password = "StrongPassword123!", Token = "invalid_token", Username = "testuser", FirstName = "Test", LastName = "User" };
            var registerSettings = new RegisterSettingsConfig { RequireInvite = true };
            _mockRegisterSettingsOptions.Setup(o => o.Value).Returns(registerSettings);
            _mockVerificationService.Setup(vs => vs.VerifyTokenAsync(It.Is<VerifyTokenRequest>(r => r.VerificationCode == "invalid_token"))).ReturnsAsync(new TokenVerificationResponse { IsVerified = false, FailureReason = "Token expired" });
            ReinitializeAuthService();
            var exception = await Assert.ThrowsExceptionAsync<InvalidTokenException>(() => _authService.RegisterUserAsync(registerRequest));
            Assert.AreEqual("Token expired", exception.Message);
            _mockVerificationService.Verify(vs => vs.VerifyTokenAsync(It.IsAny<VerifyTokenRequest>()), Times.Once);
        }

        [TestMethod]
        public async Task RegisterUserAsync_PasswordTooWeak_ThrowsRegistrationException()
        {
            var registerRequest = new RegisterRequest { Email = "test@example.com", Password = "weak", Token = "any_token", Username = "testuser", FirstName = "Test", LastName = "User" };
            var passwordSettings = new PasswordSettingsConfig { RequiredLength = 8, RequireDigit = true };
            _mockPasswordSettingsOptions.Setup(o => o.Value).Returns(passwordSettings);
            var currentRegisterSettings = _mockRegisterSettingsOptions.Object.Value;
            if (currentRegisterSettings.RequireInvite)
            {
                _mockVerificationService.Setup(vs => vs.VerifyTokenAsync(It.Is<VerifyTokenRequest>(r => r.VerificationCode == "any_token"))).ReturnsAsync(new TokenVerificationResponse { IsVerified = true, User = new User { Id = 1 } });
            }
            ReinitializeAuthService();
            var exception = await Assert.ThrowsExceptionAsync<RegistrationException>(() => _authService.RegisterUserAsync(registerRequest));
            Assert.AreEqual("Password does not meet the required complexity.", exception.Message);
        }

        [TestMethod]
        public async Task RegisterUserAsync_UserServiceFails_ThrowsRegistrationException()
        {
            var registerRequest = new RegisterRequest { Email = "test@example.com", Password = "StrongPassword123!", Token = "any_token", Username = "testuser", FirstName = "Test", LastName = "User" };
            var passwordSettings = new PasswordSettingsConfig { RequiredLength = 6 };
            _mockPasswordSettingsOptions.Setup(o => o.Value).Returns(passwordSettings);
            var currentRegisterSettings = _mockRegisterSettingsOptions.Object.Value;
            if (currentRegisterSettings.RequireInvite)
            {
                _mockVerificationService.Setup(vs => vs.VerifyTokenAsync(It.Is<VerifyTokenRequest>(r => r.VerificationCode == "any_token"))).ReturnsAsync(new TokenVerificationResponse { IsVerified = true, User = new User { Id = 1 } });
            }
            _mockUserService.Setup(us => us.RegisterUser(It.IsAny<User>())).ReturnsAsync(new RegistrationResponse { IsSuccessful = false, FailureReason = "Database error" });
            ReinitializeAuthService();
            var exception = await Assert.ThrowsExceptionAsync<RegistrationException>(() => _authService.RegisterUserAsync(registerRequest));
            Assert.AreEqual("Database error", exception.Message);
            _mockUserService.Verify(us => us.RegisterUser(It.IsAny<User>()), Times.Once);
        }

        #region Account Lockout Tests

        private void SetupLoginSettings(int maxAttempts, int lockoutDurationMinutes, bool lockoutEnabled, int cookieExpiryMinutes = 20)
        {
            var loginSettings = new LoginSettingsConfig
            {
                MaxFailedAccessAttempts = maxAttempts,
                LockoutDurationInMinutes = lockoutDurationMinutes,
                LockoutEnabled = lockoutEnabled,
                CookieExpiryMinutes = cookieExpiryMinutes
            };
            _mockLoginSettingsOptions.Setup(o => o.Value).Returns(loginSettings);
            ReinitializeAuthService();
        }

        private void MockCookieRead(string cookieName, string? protectedValue, string? unprotectedValue)
        {
            var requestCookiesMock = Mock.Get(_mockHttpContextAccessor.Object.HttpContext.Request.Cookies);
            requestCookiesMock.Setup(c => c[cookieName]).Returns(protectedValue);
            if (protectedValue != null)
            {
                _mockStringDataProtector.Setup(p => p.Unprotect(protectedValue)).Returns(unprotectedValue ?? "");
            }
        }

        private void MockCookieWrite(string cookieName, string valueToProtect, string protectedValue)
        {
            _mockStringDataProtector.Setup(p => p.Protect(valueToProtect)).Returns(protectedValue);
        }

        private void VerifyCookieDeleted(string cookieName)
        {
            var responseCookiesMock = Mock.Get(_mockHttpContextAccessor.Object.HttpContext.Response.Cookies);
            responseCookiesMock.Verify(c => c.Delete(cookieName), Times.Once);
        }

        private void VerifyCookieAppended(string cookieName, string expectedProtectedValue, TimeSpan expectedExpiryFromNow, bool isEssential = true, bool httpOnly = true, bool secure = true)
        {
            var responseCookiesMock = Mock.Get(_mockHttpContextAccessor.Object.HttpContext.Response.Cookies);
            responseCookiesMock.Verify(c => c.Append(
                cookieName,
                expectedProtectedValue,
                It.Is<CookieOptions>(options =>
                    options.HttpOnly == httpOnly &&
                    options.Secure == secure &&
                    options.IsEssential == isEssential &&
                    options.Expires.HasValue &&
                    Math.Abs((options.Expires.Value - (DateTimeOffset.UtcNow + expectedExpiryFromNow)).TotalSeconds) < 5
                )
            ), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_Successful_DeletesAttemptCookie()
        {
            SetupLoginSettings(maxAttempts: 3, lockoutDurationMinutes: 10, lockoutEnabled: true);
            var userLoginRequest = new UserLoginRequest { Identifier = "testuser", Password = "password123" };
            var user = new User { Id = 1, Username = "testuser", Email = "testuser@example.com", Salt = "salt", Password = PasswordHelper.HashPassword("password123", "salt"), Roles = new List<string> { "User" } };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(user);
            _mockSessionService.Setup(s => s.InsertSession(It.IsAny<SessionModel>())).Returns(Task.CompletedTask);
            _mockVerificationService.Setup(vs => vs.GenerateTokenAsync(user.Id, "Refresh")).ReturnsAsync("refreshtoken.value");
            MockCookieRead("LoginAttempts", "protected_attempts_value", "1");
            await _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent");
            VerifyCookieDeleted("LoginAttempts");
        }

        [TestMethod]
        public async Task LoginAsync_Failed_FirstAttempt_IncrementsAndSetsCookie()
        {
            SetupLoginSettings(maxAttempts: 3, lockoutDurationMinutes: 10, lockoutEnabled: true, cookieExpiryMinutes: 20);
            var userLoginRequest = new UserLoginRequest { Identifier = "testuser", Password = "wrongpassword" };
            var user = new User { Id = 1, Username = "testuser", Salt = "salt", Password = "correct_hashed_password" };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(user);
            MockCookieRead("LoginAttempts", null, null);
            MockCookieWrite("LoginAttempts", "1", "1_protected");
            await Assert.ThrowsExceptionAsync<InvalidCredentialsException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            VerifyCookieAppended("LoginAttempts", "1_protected", TimeSpan.FromMinutes(20));
        }

        [TestMethod]
        public async Task LoginAsync_Failed_IncrementsExistingAttemptCookie()
        {
            SetupLoginSettings(maxAttempts: 3, lockoutDurationMinutes: 10, lockoutEnabled: true, cookieExpiryMinutes: 20);
            var userLoginRequest = new UserLoginRequest { Identifier = "testuser", Password = "wrongpassword" };
            var user = new User { Id = 1, Username = "testuser", Salt = "salt", Password = "correct_hashed_password" };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(user);
            MockCookieRead("LoginAttempts", "1_protected_from_request", "1");
            MockCookieWrite("LoginAttempts", "2", "2_protected_to_response");
            await Assert.ThrowsExceptionAsync<InvalidCredentialsException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            VerifyCookieAppended("LoginAttempts", "2_protected_to_response", TimeSpan.FromMinutes(20));
        }

        [TestMethod]
        public async Task LoginAsync_Failed_ExceedsMaxAttempts_ThrowsAccountLockedException_AndSetsLockoutCookie()
        {
            int maxAttempts = 2;
            int lockoutDurationMinutes = 10;
            int cookieExpiryMinutes = 20;
            SetupLoginSettings(maxAttempts, lockoutDurationMinutes, true, cookieExpiryMinutes);
            var userLoginRequest = new UserLoginRequest { Identifier = "testuser", Password = "wrongpassword" };
            var user = new User { Id = 1, Username = "testuser", Salt = "salt", Password = "correct_hashed_password" };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(user);
            MockCookieRead("LoginAttempts", "2_protected_from_request", maxAttempts.ToString());
            string expectedProtectedCookieValue = "3_and_lockout_protected";
            _mockStringDataProtector.Reset();
            _mockResponseCookies.Reset();
            _mockStringDataProtector.Setup(p => p.Unprotect("2_protected_from_request")).Returns(maxAttempts.ToString());
            _mockStringDataProtector.Setup(p => p.Protect(It.Is<string>(s => s.StartsWith("3|")))).Callback((string s) => Console.WriteLine($"Protect called with input: {s}")).Returns(expectedProtectedCookieValue);
            var exception = await Assert.ThrowsExceptionAsync<AccountLockedException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            Assert.AreEqual($"Too many login attempts. Account locked for {lockoutDurationMinutes} minutes.", exception.Message);
        }

        [TestMethod]
        public async Task Login_Failed_WhileLockedOut_ThrowsAccountLockedException_WithCorrectRemainingTime()
        {
            int maxAttempts = 2;
            int lockoutDurationMinutes = 10;
            SetupLoginSettings(maxAttempts, lockoutDurationMinutes, true);
            var userLoginRequest = new UserLoginRequest { Identifier = "testuser", Password = "wrongpassword" };
            var user = new User { Id = 1, Username = "testuser", Salt = "salt", Password = "correct_hashed_password" };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(user);
            int currentAttemptsInCookie = maxAttempts + 1;
            DateTimeOffset lockoutExpiry = DateTimeOffset.UtcNow.AddMinutes(lockoutDurationMinutes - 2);
            string lockoutCookieValue = $"{currentAttemptsInCookie}|{lockoutExpiry.UtcTicks}";
            MockCookieRead("LoginAttempts", "locked_cookie_protected", lockoutCookieValue);
            _mockResponseCookies.Reset();
            var exception = await Assert.ThrowsExceptionAsync<AccountLockedException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            Assert.IsTrue(exception.Message.StartsWith("Account locked. Try again in"));
            Assert.IsTrue(exception.Message.EndsWith("minutes."));
            string minutesString = exception.Message.Split(" ")[5];
            Assert.IsTrue(int.TryParse(minutesString, out int reportedMinutes));
            Assert.IsTrue(reportedMinutes <= lockoutDurationMinutes - 2 && reportedMinutes > lockoutDurationMinutes - 3, $"Reported minutes {reportedMinutes} not in expected range.");
            _mockResponseCookies.Verify(c => c.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()), Times.Never);
            _mockResponseCookies.Verify(c => c.Delete(It.IsAny<string>(), It.IsAny<CookieOptions>()), Times.Never);
            _mockResponseCookies.Verify(c => c.Delete(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Login_Failed_AfterLockoutExpired_ResetsAttemptsAndAllowsLoginAttempt_ThenFailsCredentials()
        {
            int maxAttempts = 2;
            int lockoutDurationMinutes = 10;
            int cookieExpiryMinutes = 20;
            SetupLoginSettings(maxAttempts, lockoutDurationMinutes, true, cookieExpiryMinutes);
            var userLoginRequest = new UserLoginRequest { Identifier = "testuser", Password = "wrongpassword" };
            var user = new User { Id = 1, Username = "testuser", Salt = "salt", Password = "correct_hashed_password" };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(user);
            int attemptsInOldCookie = maxAttempts + 1;
            DateTimeOffset pastLockoutExpiry = DateTimeOffset.UtcNow.AddMinutes(-(lockoutDurationMinutes + 5));
            string expiredLockoutCookieValue = $"{attemptsInOldCookie}|{pastLockoutExpiry.UtcTicks}";
            MockCookieRead("LoginAttempts", "expired_lockout_cookie_protected", expiredLockoutCookieValue);
            string expectedNewAttemptValue = "1";
            string expectedProtectedNewAttemptValue = "1_after_expiry_protected";
            MockCookieWrite("LoginAttempts", expectedNewAttemptValue, expectedProtectedNewAttemptValue);
            var responseCookiesMock = Mock.Get(_mockHttpContextAccessor.Object.HttpContext.Response.Cookies);
            await Assert.ThrowsExceptionAsync<InvalidCredentialsException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            responseCookiesMock.Verify(c => c.Delete("LoginAttempts"), Times.Once);
            VerifyCookieAppended("LoginAttempts", expectedProtectedNewAttemptValue, TimeSpan.FromMinutes(cookieExpiryMinutes));
        }


        [TestMethod]
        public async Task Login_Failed_LockoutDisabled_DoesNotLockAccount_AndOnlyIncrementsAttempt()
        {
            int maxAttempts = 2;
            int lockoutDurationMinutes = 10;
            int cookieExpiryMinutes = 20;
            SetupLoginSettings(maxAttempts, lockoutDurationMinutes, false, cookieExpiryMinutes);
            var userLoginRequest = new UserLoginRequest { Identifier = "testuser", Password = "wrongpassword" };
            var user = new User { Id = 1, Username = "testuser", Salt = "salt", Password = "correct_hashed_password" };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(user);
            MockCookieRead("LoginAttempts", "2_protected_from_request_lockout_disabled", maxAttempts.ToString());
            string expectedCookieValue = (maxAttempts + 1).ToString();
            string expectedProtectedCookieValue = "3_protected_lockout_disabled";
            MockCookieWrite("LoginAttempts", expectedCookieValue, expectedProtectedCookieValue);
            await Assert.ThrowsExceptionAsync<InvalidCredentialsException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            VerifyCookieAppended("LoginAttempts", expectedProtectedCookieValue, TimeSpan.FromMinutes(cookieExpiryMinutes));
        }

        #endregion

        [TestMethod]
        public async Task LoginAsync_FirstFailedAttempt_SetsAttemptCookieCorrectly()
        {
            int maxAttempts = 3;
            int cookieExpiryMinutes = 20;
            SetupLoginSettings(maxAttempts, 10, true, cookieExpiryMinutes);
            var userLoginRequest = new UserLoginRequest { Identifier = "testuser", Password = "wrongpassword" };
            var user = new User { Id = 1, Username = "testuser", Salt = "salt", Password = PasswordHelper.HashPassword("correctPassword", "salt") };
            _mockUserService.Setup(us => us.GetUser(userLoginRequest.Identifier)).ReturnsAsync(user);
            var requestCookiesMock = Mock.Get(_mockHttpContextAccessor.Object.HttpContext.Request.Cookies);
            requestCookiesMock.Setup(c => c["LoginAttempts"]).Returns((string)null);
            _mockStringDataProtector.Setup(p => p.Unprotect(null)).Returns("0");
            _mockStringDataProtector.Setup(p => p.Unprotect(string.Empty)).Returns("0");
            string expectedProtectedValue = "protected_1_attempt";
            _mockStringDataProtector.Setup(p => p.Protect("1")).Returns(expectedProtectedValue);
            _mockResponseCookies.Setup(c => c.Append("LoginAttempts", expectedProtectedValue, It.IsAny<CookieOptions>()));
            await Assert.ThrowsExceptionAsync<InvalidCredentialsException>(() => _authService.LoginAsync(userLoginRequest, "127.0.0.1", "TestAgent"));
            _mockResponseCookies.Verify(c => c.Append("LoginAttempts", expectedProtectedValue, It.IsAny<CookieOptions>()), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_ShouldLockAccount_AfterExceedingMaxFailedAttempts()
        {
            var loginSettingsConfig = new LoginSettingsConfig { MaxFailedAccessAttempts = 3, LockoutEnabled = true, CookieExpiryMinutes = 10, LockoutDurationInMinutes = 15 };
            _mockLoginSettingsOptions.Setup(s => s.Value).Returns(loginSettingsConfig);
            ReinitializeAuthService();
            _mockUserService.Setup(s => s.GetUser(It.IsAny<string>())).ReturnsAsync(() => new User { Id = 1, Username = "testuser", Email = "test@example.com", Salt = "testsalt", Password = PasswordHelper.HashPassword("correctpassword", "testsalt"), Roles = new List<string> { "User" } });
            _mockSettingsService.Setup(s => s.GetAllSettingsAsync(It.IsAny<int?>())).ReturnsAsync(new List<Setting>());
            var mockHttpContext = _mockHttpContextAccessor.Object.HttpContext;
            var requestCookiesDict = new Dictionary<string, string>();
            var reqCookiesMock = Mock.Get(mockHttpContext.Request.Cookies);
            reqCookiesMock.Setup(c => c[It.IsAny<string>()]).Returns((string key) => requestCookiesDict.TryGetValue(key, out var value) ? value : null);
            var respCookiesMock = Mock.Get(mockHttpContext.Response.Cookies);
            respCookiesMock.Setup(c => c.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>())).Callback<string, string, CookieOptions>((key, value, options) => { requestCookiesDict[key] = value;  });
            respCookiesMock.Setup(c => c.Delete(It.IsAny<string>())).Callback<string>(key => { requestCookiesDict.Remove(key); });
            _mockStringDataProtector.Reset();
            _mockStringDataProtector.Setup(p => p.Protect(It.IsAny<string>())).Returns((string s) => { if (s.StartsWith("4|")) { return "SLOCKED_FROM_ANY"; } return "UNEXPECTED_PROTECT_INPUT_" + s; });
            _mockStringDataProtector.Setup(p => p.Protect("1")).Returns("S1").Callback(() => Console.WriteLine($"Protect CS 1: input='1' -> S1"));
            _mockStringDataProtector.Setup(p => p.Protect("2")).Returns("S2").Callback(() => Console.WriteLine($"Protect CS 2: input='2' -> S2"));
            _mockStringDataProtector.Setup(p => p.Protect("3")).Returns("S3").Callback(() => Console.WriteLine($"Protect CS 3: input='3' -> S3"));
            _mockStringDataProtector.Setup(p => p.Unprotect("S1")).Returns("1").Callback(() => Console.WriteLine($"Unprotect CS S1: input='S1' -> 1"));
            _mockStringDataProtector.Setup(p => p.Unprotect("S2")).Returns("2").Callback(() => Console.WriteLine($"Unprotect CS S2: input='S2' -> 2"));
            _mockStringDataProtector.Setup(p => p.Unprotect("S3")).Returns("3").Callback(() => Console.WriteLine($"Unprotect CS S3: input='S3' -> 3"));
            _mockStringDataProtector.Setup(p => p.Unprotect("SLOCKED_FROM_ANY")).Returns("4|HARCODED_TS_FOR_SPLIT_TEST").Callback(() => Console.WriteLine($"Unprotect CS SLOCKED_FROM_ANY: input='SLOCKED_FROM_ANY' -> 4|HARCODED_TS_FOR_SPLIT_TEST"));
            _mockStringDataProtector.Setup(p => p.Unprotect(It.Is<string>(s => s == null))).Returns(() => { return "NULL_FALLBACK_RETURN"; });
            _mockStringDataProtector.Setup(p => p.Unprotect(It.Is<string>(s => s != null && s != "S1" && s != "S2" && s != "S3" && s != "SLOCKED_FROM_ANY" && s != "DEFAULT_PROTECT_FROM_ANY"))).Returns<string>(s => { return s + "_FALLBACK_RETURN"; });
            var loginRequest = new UserLoginRequest { Identifier = "testuser", Password = "wrongpassword" };
            int maxAttempts = loginSettingsConfig.MaxFailedAccessAttempts;
            for (int i = 1; i <= maxAttempts; i++)
            {
                var ex = await Assert.ThrowsExceptionAsync<InvalidCredentialsException>(() => _authService.LoginAsync(loginRequest, "127.0.0.1", "test-agent"));
                Assert.IsNotNull(requestCookiesDict["LoginAttempts"], $"Cookie should be set on attempt {i}");
                string protectedCookie = requestCookiesDict["LoginAttempts"];
                string cookieValue = _mockStringDataProtector.Object.Unprotect(protectedCookie);
                Assert.AreEqual(i.ToString(), cookieValue, $"Cookie attempt count mismatch on attempt {i}");
            }
            var lockedException = await Assert.ThrowsExceptionAsync<AccountLockedException>(() => _authService.LoginAsync(loginRequest, "127.0.0.1", "test-agent"));
            Assert.IsTrue(lockedException.Message.Contains("Account locked"), "Exception message does not indicate account locked.");
        }
    }
}

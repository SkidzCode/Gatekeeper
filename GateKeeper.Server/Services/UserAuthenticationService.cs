using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GateKeeper.Server.Interface;
using Microsoft.IdentityModel.Tokens;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using GateKeeper.Server.Interface;
using System.Xml;
using GateKeeper.Server.Models.Account;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity.Data;
using RegisterRequest = GateKeeper.Server.Models.Account.UserModels.RegisterRequest;
using GateKeeper.Server.Models.Site;
using System.Runtime.InteropServices;
using System.Security;
using GateKeeper.Server.Models.Account.Login;
using Microsoft.AspNetCore.DataProtection;
using GateKeeper.Server.Models.Account.UserModels;

namespace GateKeeper.Server.Services
{
    /// <summary>
    /// Service handling user authentication and related operations.
    /// </summary>
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly IDataProtector _protector;
        private IHttpContextAccessor _httpContextAccessor;

        private readonly IDbHelper _dbHelper;
        private readonly ILogger<UserAuthenticationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IVerifyTokenService _verificationService;
        private readonly IUserService _userService;
        private readonly ISettingsService _settingsService;
        private readonly IKeyManagementService _keyManagementService;
        private readonly INotificationTemplateService _notificationTemplateService;
        private readonly INotificationService _notificationService;
        private readonly ISessionService _sessionService;
        private readonly bool _requiresInvite;

        private const string cookieName = "LoginAttempts";


        /// <summary>
        /// Constructor for UserAuthenticationService.
        /// </summary>
        /// <param name="userService">User-related operations.</param>
        /// <param name="verificationService">Verification token operations.</param>
        /// <param name="configuration">App configuration (used for time-based configs, etc.).</param>
        /// <param name="dbHelper">Database wrapper for DB access.</param>
        /// <param name="logger">Logger for logging information and errors.</param>
        /// <param name="settingsService">Service to retrieve settings for users.</param>
        /// <param name="keyManagementService">Key Management Service for retrieving rotating JWT keys.</param>
        /// <param name="protector"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="notification"></param>
        /// <param name="notificationTemplateService"></param>
        public UserAuthenticationService(
            IUserService userService,
            IVerifyTokenService verificationService,
            IConfiguration configuration,
            IDbHelper dbHelper,
            ILogger<UserAuthenticationService> logger,
            ISettingsService settingsService,
            IKeyManagementService keyManagementService,
            IDataProtectionProvider protector, 
            IHttpContextAccessor httpContextAccessor, 
            INotificationService notification,
            INotificationTemplateService notificationTemplateService,
            ISessionService sessionService)
        {
            _configuration = configuration;
            _dbHelper = dbHelper;
            _logger = logger;
            _verificationService = verificationService;
            _userService = userService;
            _settingsService = settingsService;
            _keyManagementService = keyManagementService;
            _protector = protector.CreateProtector("SecureCookies"); ;
            _httpContextAccessor = httpContextAccessor;
            _requiresInvite = _configuration.GetValue<bool>("RegisterSettings:RequireInvite");
            _notificationService = notification;
            _notificationTemplateService = notificationTemplateService;
            _sessionService = sessionService;
        }

        /// <inheritdoc />
        public async Task<RegistrationResponse> RegisterUserAsync(RegisterRequest registerRequest)
        {
            RegistrationResponse response = new RegistrationResponse();

            registerRequest.Token = registerRequest.Token.Replace(" ", "+");
            response.User = new User()
            {
                FirstName = registerRequest.FirstName,
                Email = registerRequest.Email,
                LastName = registerRequest.LastName,
                Password = registerRequest.Password,
                Username = registerRequest.Username,
                Phone = registerRequest.Phone
            };

            TokenVerificationResponse tokenResponse = new TokenVerificationResponse();
            if (_requiresInvite)
            {
                if (registerRequest.Token.Length > 0)
                {
                    tokenResponse = await _verificationService.VerifyTokenAsync(new VerifyTokenRequest()
                    {
                        TokenType = "Invite",
                        VerificationCode = registerRequest.Token
                    });

                    if (!tokenResponse.IsVerified)
                    {
                        response.FailureReason = tokenResponse.FailureReason;
                        return response;
                    }
                }
            }

            if (!await ValidatePasswordStrengthAsync(registerRequest.Password))
            {
                response.FailureReason = "Password does not meet the required complexity.";
                return response;
            }

            response = await _userService.RegisterUser(response.User);

            if (!response.IsSuccessful)
                return response;

            await AssignRoleToUser(response.User.Id, "NewUser");
            string token = await _verificationService.GenerateTokenAsync(response.User.Id, "NewUser");

            var template = await _notificationTemplateService.GetNotificationTemplateByNameAsync("Verify Email Template");
            int noticeId = await _notificationService.InsertNotificationAsync(new Notification()
            {
                Channel = "Email",
                Message = template.Body,
                Subject = template.Subject,
                RecipientId = response.User.Id,
                TokenType = "Verification",
                URL = registerRequest.Website,
                FromId = response.User.Id,
                ToEmail = registerRequest.Email,
                ToName = $"{registerRequest.FirstName} {registerRequest.LastName}"
            });
            
            if (_requiresInvite)
            {
                await _verificationService.RevokeTokensAsync(tokenResponse.User.Id, "Invite", registerRequest.Token);
            }

            return response;
        }

        /// <inheritdoc />
        public async Task<LoginResponse> LoginAsync(UserLoginRequest userLogin, string ipAddress, string userAgent)
        {
            LoginResponse response = new LoginResponse();

            response.User = await _userService.GetUser(userLogin.Identifier);
            response = await LoginAttempts(response);

            if (response.ToMany)
                return response;

            int? userId = response.User?.Id ?? null;
            
            List<Setting> theSettings = await _settingsService.GetAllSettingsAsync(userId);
            response.Settings = theSettings.Where(s => s.UserId == null).ToList();
            List<Setting> userSettings = theSettings
                .GroupBy(s => new { s.Name, s.Category })
                .SelectMany(group =>
                {
                    // If any setting in the group has a UserId, exclude the ones without a UserId
                    if (group.Any(s => s.UserId.HasValue))
                        return group.Where(s => s.UserId.HasValue);
                    // Otherwise, include all
                    return group;
                })
                .ToList();

            if (response.User == null || string.IsNullOrEmpty(response.User.Salt) ||
                string.IsNullOrEmpty(response.User.Password))
            {
                response.FailureReason = "User not found";
                return response;
            }

            var hashedPassword = PasswordHelper.HashPassword(userLogin.Password, response.User.Salt);

            if (hashedPassword != response.User.Password)
            {
                await response.User.ClearPHIAsync();
                response.FailureReason = "Invalid credentials";
                return response;
            }

            // Generate tokens
            response.AccessToken = await GenerateJwtToken(response.User);
            response.RefreshToken = await _verificationService.GenerateTokenAsync(response.User.Id, "Refresh");
            response.VerificationId = response.RefreshToken.Split('.')[0];
            response.IsSuccessful = true;
            response.Settings = userSettings;
            await DeleteCookie();

            var currentSession = new SessionModel()
            {
                Id = Guid.NewGuid().ToString(),
                UserId = response.User.Id,
                VerificationId = response.VerificationId,
                ExpiryDate = DateTime.UtcNow.AddDays(15),
                Complete = false,
                Revoked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _sessionService.InsertSession(currentSession);
            response.SessionId = currentSession.Id;
            
            return response;
        }

        /// <inheritdoc />
        public async Task<int> LogoutAsync(string? token = null, int userId = 0)
        {
            try
            {
                if (token == null || !token.Contains('.'))
                    throw new Exception("Invalid token format.");
                await _sessionService.LogoutToken(token, userId);
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<TokenVerificationResponse> VerifyNewUser(string verificationCode)
        {
            TokenVerificationResponse response = new(); 
            response = await _verificationService.VerifyTokenAsync(
                new VerifyTokenRequest()
                {
                    VerificationCode = verificationCode,
                    TokenType = "NewUser",
                });


            if (!response.IsVerified) return response;

            await using var connection = await _dbHelper.GetWrapperAsync();
            await connection.ExecuteNonQueryAsync("ValidateFinish", CommandType.StoredProcedure,
                new MySqlParameter("@p_UserId", response.User?.Id),
                new MySqlParameter("@p_Id", response.SessionId));

            return response;
        }

        /// <inheritdoc />
        public async Task<LoginResponse> RefreshTokensAsync(string refreshToken)
        {
            LoginResponse response = new LoginResponse();
            var response2 = await _verificationService.VerifyTokenAsync(new VerifyTokenRequest()
            {
                TokenType = "Refresh",
                VerificationCode = refreshToken
            });

            response.User = response2.User;
            response = await LoginAttempts(response);

            if (response.ToMany) return response;

            int? userId = response.User?.Id ?? null;
            List<Setting> theSettings = await _settingsService.GetAllSettingsAsync(userId);
            response.Settings = theSettings.Where(s => s.UserId == null).ToList();
            List<Setting> userSettings = theSettings
                .GroupBy(s => new { s.Name, s.Category })
                .SelectMany(group =>
                {
                    // If any setting in the group has a UserId, exclude the ones without a UserId
                    if (group.Any(s => s.UserId.HasValue))
                    {
                        return group.Where(s => s.UserId.HasValue);
                    }

                    // Otherwise, include all
                    return group;
                })
                .ToList();

            if (!response2.IsVerified || response.User == null)
            {
                response.FailureReason = response2.FailureReason;
                if (response.User != null)
                    await response.User.ClearPHIAsync();
                return response;
            }

            await _verificationService.RevokeTokensAsync(response.User.Id, "Refresh", refreshToken);
            // Generate new tokens
            response.AccessToken = await GenerateJwtToken(response.User);
            response.RefreshToken = await _verificationService.GenerateTokenAsync(response.User.Id, "Refresh");
            response.VerificationId = response.RefreshToken.Split('.')[0];
            response.IsSuccessful = true;
            response.Settings = userSettings;
            await DeleteCookie();

            response.SessionId = await _sessionService.RefreshSession(response.User.Id, refreshToken.Split('.')[0], response.VerificationId);
            return response;
        }

        public async Task InitiatePasswordResetAsync(User user, InitiatePasswordResetRequest initiateRequest)
        {
            try
            {
                var template = await _notificationTemplateService.GetNotificationTemplateByNameAsync("Reset Password");
                int noticeId = await _notificationService.InsertNotificationAsync(new Notification()
                {
                    Channel = "Email",
                    Message = template.Body,
                    Subject = template.Subject,
                    RecipientId = user.Id,
                    TokenType = "ForgotPassword",
                    URL = initiateRequest.Website,
                    FromId = user.Id,
                    ToEmail = user.Email,
                    ToName = $"{user.FirstName} {user.LastName}"
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating password reset.");
                throw;
            }
        }


        /// <inheritdoc />
        public async Task<TokenVerificationResponse> ResetPasswordAsync(PasswordResetRequest resetRequest)
        {
            var response =
                await _verificationService.VerifyTokenAsync(new VerifyTokenRequest()
                {
                    TokenType = "ForgotPassword",
                    VerificationCode = resetRequest.ResetToken
                });

            if (!response.IsVerified) return response;

            await _userService.ChangePassword(response.User.Id, resetRequest.NewPassword);
            return response;
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _userService.UsernameExistsAsync(username);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _userService.EmailExistsAsync(email);
        }

        /// <inheritdoc />
        public async Task<bool> ValidatePasswordStrengthAsync(string password)
        {
            return await PasswordHelper.ValidatePasswordStrengthAsync(_configuration, password);
        }
        
        /// <inheritdoc />
        public async Task<bool> LogoutFromDeviceAsync(int userId, string sessionId)
        {
            await _sessionService.LogoutSession(sessionId, userId);
            return true;
        }

        #region Private Helper Methods

        /// <summary>
        /// Generates a JWT access token for the authenticated user, using a rotating secret key.
        /// </summary>
        /// <param name="user">Authenticated user details.</param>
        /// <returns>JWT access token as a string.</returns>
        private async Task<string> GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // 1) Retrieve the current signing key from the key-management service (as a SecureString).
            SecureString secureKey = await _keyManagementService.GetCurrentKeyAsync();
            if (secureKey == null)
                throw new InvalidOperationException("No active signing key available.");

            // 2) Convert SecureString to byte[] (this includes base64 decoding).
            byte[] keyBytes;
            IntPtr bstrPtr = Marshal.SecureStringToBSTR(secureKey);
            try
            {
                string base64Key = Marshal.PtrToStringBSTR(bstrPtr);
                keyBytes = Convert.FromBase64String(base64Key);
            }
            finally
            {
                // Always free the BSTR memory
                Marshal.ZeroFreeBSTR(bstrPtr);
            }

            // 3) Construct the user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
            };

            // Add roles as separate claims
            if (user.Roles != null)
            {
                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            // 4) Create signing credentials
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // 5) Build token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                // Duration from config (fallback: 60 minutes if not found)
                Expires = DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["JwtConfig:ExpirationMinutes"] ?? "60")
                ),
                SigningCredentials = creds,
                Issuer = _configuration["JwtConfig:Issuer"],
                Audience = _configuration["JwtConfig:Audience"]
            };

            // 6) Create the token
            var token = tokenHandler.CreateToken(tokenDescriptor);
            string jwt = tokenHandler.WriteToken(token);

            // 7) Zero out the key bytes from memory once we're done
            Array.Clear(keyBytes, 0, keyBytes.Length);

            return jwt;
        }

        /// <summary>
        /// Assigns a default role to the newly registered user.
        /// </summary>
        /// <param name="connection">Active database connection.</param>
        /// <param name="userId">ID of the user to assign the role to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task AssignRoleToUser(int userId, string role)
        {
            await using var connection = await _dbHelper.GetWrapperAsync();
            await connection.ExecuteNonQueryAsync("AssignRoleToUser", CommandType.StoredProcedure,
                new MySqlParameter("@p_UserId", userId),
                new MySqlParameter("@p_RoleName", role));
        }

        private async Task<LoginResponse> LoginAttempts(LoginResponse loginResponse)
        {
            
            if (_httpContextAccessor.HttpContext == null) return loginResponse;

            int maxAttempts = Convert.ToInt32(_configuration["LoginSettings:MaxFailedAttempts"]);
            int cookieExpiryMinutes = Convert.ToInt32(_configuration["LoginSettings:CookieExpires"]); ;
            bool lockoutEnabled = Convert.ToBoolean(_configuration["LoginSettings:LockoutEnabled"]); ;

            // Read the current attempt count from encrypted cookie
            var attemptsCookie = _httpContextAccessor.HttpContext.Request.Cookies[cookieName];
            if (!string.IsNullOrEmpty(attemptsCookie))
            {
                try
                {
                    // Decrypt the cookie value
                    var decryptedValue = _protector.Unprotect(attemptsCookie);
                    if (int.TryParse(decryptedValue, out int parsedAttempts))
                        loginResponse.Attempts = parsedAttempts;
                }
                catch
                {
                    // If decryption fails or data is tampered, we treat it as invalid
                    // (Optionally reset to 0 or handle differently)
                    loginResponse.Attempts = 100;
                }
            }

            // Increment and protect (encrypt) the new attempt count
            loginResponse.Attempts++;
            var encryptedAttempts = _protector.Protect(loginResponse.Attempts.ToString());

            // Build secure cookie options
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(cookieExpiryMinutes),
                HttpOnly = true,      // Prevents JavaScript from accessing the cookie
                Secure = true,        // Ensures the cookie is only sent over HTTPS
                IsEssential = true    // Ensures the cookie is sent even if the user hasn't consented
            };

            // Write the updated (encrypted) attempts back to the cookie
            _httpContextAccessor.HttpContext.Response.Cookies.Append(cookieName, encryptedAttempts, cookieOptions);
            loginResponse.ToMany = loginResponse.Attempts > maxAttempts;

            if (!loginResponse.ToMany) return loginResponse;

            if (loginResponse.User != null)
                await loginResponse.User.ClearPHIAsync();
            loginResponse.FailureReason = "Too many login attempts";
            return loginResponse;
        }

        private async Task DeleteCookie()
        {
            if (_httpContextAccessor.HttpContext == null) return;

            // If login is successful, remove the cookie to reset attempts
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(cookieName);
        }

        #endregion
    }
}

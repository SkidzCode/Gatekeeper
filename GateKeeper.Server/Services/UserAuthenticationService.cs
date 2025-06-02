using Microsoft.Extensions.Logging;
using MySqlConnector;
using Microsoft.Extensions.Options; // Added for IOptions
using GateKeeper.Server.Models.Configuration; // Added for typed configuration classes
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
using GateKeeper.Server.Exceptions;

namespace GateKeeper.Server.Services
{
    /// <summary>
    /// Service handling user authentication and related operations.
    /// </summary>
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly IStringDataProtector _protector; // Changed type
        private IHttpContextAccessor _httpContextAccessor;

        private readonly IDbHelper _dbHelper;
        private readonly ILogger<UserAuthenticationService> _logger;
        private readonly JwtSettingsConfig _jwtSettings;
        private readonly PasswordSettingsConfig _passwordSettings;
        private readonly RegisterSettingsConfig _registerSettings;
        private readonly LoginSettingsConfig _loginSettings;
        private readonly IVerifyTokenService _verificationService;
        private readonly IUserService _userService;
        private readonly ISettingsService _settingsService;
        private readonly IKeyManagementService _keyManagementService;
        private readonly INotificationTemplateService _notificationTemplateService;
        private readonly INotificationService _notificationService;
        private readonly ISessionService _sessionService;
        // private readonly bool _requiresInvite; // This will now come from _registerSettings

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
        /// <param name="stringDataProtector">String data protector service.</param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="notification"></param>
        /// <param name="notificationTemplateService"></param>
        public UserAuthenticationService(
            IUserService userService,
            IVerifyTokenService verificationService,
            IOptions<JwtSettingsConfig> jwtSettingsOptions,
            IOptions<PasswordSettingsConfig> passwordSettingsOptions,
            IOptions<RegisterSettingsConfig> registerSettingsOptions,
            IOptions<LoginSettingsConfig> loginSettingsOptions,
            IDbHelper dbHelper,
            ILogger<UserAuthenticationService> logger,
            ISettingsService settingsService,
            IKeyManagementService keyManagementService,
            IStringDataProtector stringDataProtector, // Changed parameter type
            IHttpContextAccessor httpContextAccessor, 
            INotificationService notification,
            INotificationTemplateService notificationTemplateService,
            ISessionService sessionService)
        {
            _jwtSettings = jwtSettingsOptions.Value;
            _passwordSettings = passwordSettingsOptions.Value;
            _registerSettings = registerSettingsOptions.Value;
            _loginSettings = loginSettingsOptions.Value;
            _dbHelper = dbHelper;
            _logger = logger;
            _verificationService = verificationService;
            _userService = userService;
            _settingsService = settingsService;
            _keyManagementService = keyManagementService;
            _protector = stringDataProtector;
            _httpContextAccessor = httpContextAccessor;
            // _requiresInvite = _configuration.GetValue<bool>("RegisterSettings:RequireInvite"); // Now from _registerSettings.RequireInvite
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
            if (_registerSettings.RequireInvite) // Use _registerSettings
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
                        throw new InvalidTokenException(tokenResponse.FailureReason ?? "Invalid invite token.");
                    }
                }
            }

            if (!await ValidatePasswordStrengthAsync(registerRequest.Password))
            {
                throw new RegistrationException("Password does not meet the required complexity.");
            }

            var userServiceResponse = await _userService.RegisterUser(response.User);

            if (!userServiceResponse.IsSuccessful)
            {
                throw new RegistrationException(userServiceResponse.FailureReason ?? "User registration failed due to an unknown reason.");
            }
            response.User = userServiceResponse.User; // Ensure the user object in the response is the one from the service

            await AssignRoleToUser(response.User.Id, "NewUser");

            var template = await _notificationTemplateService.GetNotificationTemplateByNameAsync("Verify Email Template");
            await _notificationService.InsertNotificationAsync(new Notification()
            {
                Channel = "Email",
                Message = template.Body,
                Subject = template.Subject,
                RecipientId = response.User.Id,
                TokenType = "NewUser",
                URL = registerRequest.Website,
                FromId = response.User.Id,
                ToEmail = registerRequest.Email,
                ToName = $"{registerRequest.FirstName} {registerRequest.LastName}"
            });
            
            if (_registerSettings.RequireInvite) // Use _registerSettings
            {
                await _verificationService.CompleteTokensAsync(tokenResponse.User.Id, "Invite", registerRequest.Token);
            }

            return response;
        }

        /// <inheritdoc />
        public async Task<LoginResponse> LoginAsync(UserLoginRequest userLogin, string ipAddress, string userAgent)
        {
            LoginResponse response = new LoginResponse();

            response.User = await _userService.GetUser(userLogin.Identifier);
            await UpdateLoginAttemptsAndThrowIfLockedAsync(response.User, response);

            // User Not Found Condition
            if (response.User == null || string.IsNullOrEmpty(response.User.Salt) ||
                string.IsNullOrEmpty(response.User.Password))
            {
                // Ensure ClearPHIAsync is NOT called here as response.User might be null
                throw new UserNotFoundException($"User '{userLogin.Identifier}' not found or essential data missing.");
            }
            
            int? userId = response.User?.Id ?? null; // userId can be safely accessed now
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

            var hashedPassword = PasswordHelper.HashPassword(userLogin.Password, response.User.Salt);

            // Invalid Credentials Condition
            if (hashedPassword != response.User.Password)
            {
                await response.User.ClearPHIAsync();
                throw new InvalidCredentialsException("Invalid username or password.");
            }

            // Generate tokens
            response.AccessToken = await GenerateJwtToken(response.User);
            response.RefreshToken = await _verificationService.GenerateTokenAsync(response.User.Id, "Refresh");
            response.VerificationId = response.RefreshToken.Split('.')[0];
            // response.IsSuccessful = true; // Removed as success is indicated by not throwing
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
                if (string.IsNullOrEmpty(token) || !token.Contains('.')) // Added IsNullOrEmpty for robustness
                    throw new InvalidTokenException("Invalid token format.");
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


            if (!response.IsVerified)
            {
                throw new InvalidTokenException(response.FailureReason ?? "Invalid verification code.");
            }

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
            await UpdateLoginAttemptsAndThrowIfLockedAsync(response.User, response);

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
                // It's important not to call ClearPHIAsync if response.User is null.
                // If response.User is null, ClearPHIAsync would throw a NullReferenceException.
                // If response2.IsVerified is false, but user is not null, then we might clear PHI.
                if (response.User != null && !response2.IsVerified)
                {
                     await response.User.ClearPHIAsync();
                }
                throw new InvalidTokenException(response2.FailureReason ?? "Invalid refresh token or user not found for token.");
            }

            await _verificationService.RevokeTokensAsync(response.User.Id, "Refresh", refreshToken);
            // Generate new tokens
            response.AccessToken = await GenerateJwtToken(response.User);
            response.RefreshToken = await _verificationService.GenerateTokenAsync(response.User.Id, "Refresh");
            response.VerificationId = response.RefreshToken.Split('.')[0];
            // response.IsSuccessful = true; // Removed
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
                await _notificationService.InsertNotificationAsync(new Notification()
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

            if (!response.IsVerified)
            {
                throw new InvalidTokenException(response.FailureReason ?? "Invalid password reset token.");
            }

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
            // Pass the _passwordSettings directly to the helper.
            return await PasswordHelper.ValidatePasswordStrengthAsync(_passwordSettings, password);
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
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.TokenValidityInMinutes), // Use JwtSettingsConfig
                SigningCredentials = creds,
                Issuer = _jwtSettings.Issuer, // Use JwtSettingsConfig
                Audience = _jwtSettings.Audience // Use JwtSettingsConfig
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

        private async Task UpdateLoginAttemptsAndThrowIfLockedAsync(User? user, LoginResponse loginResponse)
        {
            if (_httpContextAccessor.HttpContext == null) return;

            // Use _loginSettings
            int maxAttempts = _loginSettings.MaxFailedAccessAttempts;
            int cookieExpiryMinutes = _loginSettings.CookieExpiryMinutes;
            // bool lockoutEnabled = _loginSettings.LockoutEnabled; // LockoutEnabled is not directly used in the following logic, but MaxFailedAccessAttempts implies it.

            // Read the current attempt count from encrypted cookie
            var attemptsCookie = _httpContextAccessor.HttpContext.Request.Cookies[cookieName];
            if (!string.IsNullOrEmpty(attemptsCookie))
            {
                try
                {
                    // Decrypt the cookie value
                    var decryptedValue = _protector.Unprotect(attemptsCookie); // Now uses string version
                    if (int.TryParse(decryptedValue, out int parsedAttempts))
                        loginResponse.Attempts = parsedAttempts;
                }
                catch
                {
                    // If decryption fails or data is tampered, we treat it as invalid
                    // (Optionally reset to 0 or handle differently)
                    // Setting to a high number to likely trigger lockout on next attempt if cookie was tampered.
                    loginResponse.Attempts = maxAttempts + 1;
                }
            }

            // Increment and protect (encrypt) the new attempt count
            loginResponse.Attempts++;
            var encryptedAttempts = _protector.Protect(loginResponse.Attempts.ToString()); // Now uses string version

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

            if (loginResponse.ToMany)
            {
                if (user != null) // Use the passed user object
                    await user.ClearPHIAsync();
                throw new AccountLockedException("Too many login attempts. Account locked.");
            }
            // No return loginResponse; as the method is void
        }

        // OriginalLoginAttemptsForRefreshAsync method removed

        private async Task DeleteCookie()
        {
            if (_httpContextAccessor.HttpContext == null) return;

            // If login is successful, remove the cookie to reset attempts
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(cookieName);
        }

        #endregion
    }
}

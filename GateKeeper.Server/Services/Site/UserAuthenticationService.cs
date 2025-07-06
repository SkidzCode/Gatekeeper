using Microsoft.Extensions.Options;
using GateKeeper.Server.Models.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using GateKeeper.Server.Interface;
using Microsoft.IdentityModel.Tokens;
using GateKeeper.Server.Models.Account;
using RegisterRequest = GateKeeper.Server.Models.Account.UserModels.RegisterRequest;
using GateKeeper.Server.Models.Site;
using System.Runtime.InteropServices;
using System.Security;
using GateKeeper.Server.Models.Account.Login;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Exceptions;

namespace GateKeeper.Server.Services.Site
{
    /// <summary>
    /// Service handling user authentication and related operations.
    /// </summary>
    public class UserAuthenticationService(
        IUserService userService,
        IVerifyTokenService verificationService,
        IOptions<JwtSettingsConfig> jwtSettingsOptions,
        IOptions<PasswordSettingsConfig> passwordSettingsOptions,
        IOptions<RegisterSettingsConfig> registerSettingsOptions,
        IOptions<LoginSettingsConfig> loginSettingsOptions,
        ILogger<UserAuthenticationService> logger,
        ISettingsService settingsService,
        IKeyManagementService keyManagementService,
        IStringDataProtector stringDataProtector,
        IHttpContextAccessor httpContextAccessor,
        INotificationService notification,
        INotificationTemplateService notificationTemplateService,
        ISessionService sessionService,
        IUserAuthenticationRepository userAuthRepository) : IUserAuthenticationService
    {
        private readonly IStringDataProtector _protector = stringDataProtector;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly ILogger<UserAuthenticationService> _logger = logger;
        private readonly JwtSettingsConfig _jwtSettings = jwtSettingsOptions.Value;
        private readonly PasswordSettingsConfig _passwordSettings = passwordSettingsOptions.Value;
        private readonly RegisterSettingsConfig _registerSettings = registerSettingsOptions.Value;
        private readonly LoginSettingsConfig _loginSettings = loginSettingsOptions.Value;
        private readonly IVerifyTokenService _verificationService = verificationService;
        private readonly IUserService _userService = userService;
        private readonly ISettingsService _settingsService = settingsService;
        private readonly IKeyManagementService _keyManagementService = keyManagementService;
        private readonly INotificationTemplateService _notificationTemplateService = notificationTemplateService;
        private readonly INotificationService _notificationService = notification;
        private readonly ISessionService _sessionService = sessionService;
        private readonly IUserAuthenticationRepository _userAuthRepository = userAuthRepository;

        private const string CookieName = "LoginAttempts";

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

            if (tokenResponse.User == null)
            {
                _logger.LogError("Token verification failed or user not found for token: {Token}", registerRequest.Token);
                throw new RegistrationException("Token verification failed or user not found for token.");
            }

            if (!await ValidatePasswordStrengthAsync(registerRequest.Password))
            {
                throw new RegistrationException("Password does not meet the required complexity.");
            }

            var userServiceResponse = await _userService.RegisterUser(response.User);

            if (!userServiceResponse.IsSuccessful)
            {
                throw new RegistrationException(
                    !string.IsNullOrEmpty(userServiceResponse.FailureReason) ? 
                        userServiceResponse.FailureReason : 
                        "User registration failed due to an unknown reason.");
            }
            response.User = userServiceResponse.User; // Ensure the user object in the response is the one from the service

            await _userAuthRepository.AssignRoleToUserAsync(response.User.Id, "NewUser"); // Changed

            var template = await _notificationTemplateService.GetNotificationTemplateByNameAsync("Verify Email Template");
            if (template == null)
            {
                _logger.LogError("Notification template 'Verify Email Template' not found.");
                throw new RegistrationException("Email verification template not found.");
            }
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
            LoginResponse response = new LoginResponse
            {
                User = await _userService.GetUser(userLogin.Identifier)
            };

            await UpdateLoginAttemptsAndThrowIfLockedAsync(response.User, response);

            // User Not Found Condition
            if (response.User == null || string.IsNullOrEmpty(response.User.Salt) ||
                string.IsNullOrEmpty(response.User.Password))
            {
                // Ensure ClearPHIAsync is NOT called here as response.User might be null
                throw new UserNotFoundException($"User '{userLogin.Identifier}' not found or essential data missing.");
            }
            
            var hashedPassword = PasswordHelper.HashPassword(userLogin.Password, response.User.Salt);

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
            DeleteCookie();

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
            TokenVerificationResponse response = await _verificationService.VerifyTokenAsync(
                new VerifyTokenRequest()
                {
                    VerificationCode = verificationCode,
                    TokenType = "NewUser",
                });


            if (!response.IsVerified)
            {
                throw new InvalidTokenException(response.FailureReason ?? "Invalid verification code.");
            }

            // Ensure response.User and response.SessionId are not null before proceeding
            if (response.User == null || response.User.Id == 0 || string.IsNullOrEmpty(response.SessionId))
            {
                // This case should ideally be handled by VerifyTokenAsync throwing an exception or returning IsVerified = false
                // Adding a specific log and exception here for robustness.
                _logger.LogError("User or SessionId is null after token verification, which should not happen for a verified token. UserId: {UserId}, SessionId: {SessionId}", response.User?.Id, response.SessionId);
                throw new InvalidOperationException("User details or session ID missing after successful token verification.");
            }
            await _userAuthRepository.ValidateFinishAsync(response.User.Id, response.SessionId); // Changed

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
            DeleteCookie();

            response.SessionId = await _sessionService.RefreshSession(response.User.Id, refreshToken.Split('.')[0], response.VerificationId);
            return response;
        }

        public async Task InitiatePasswordResetAsync(User user, InitiatePasswordResetRequest initiateRequest)
        {
            try
            {
                var template = await _notificationTemplateService.GetNotificationTemplateByNameAsync("Reset Password");
                if (template == null)
                {
                    _logger.LogError("Notification template 'Reset Password' not found.");
                    throw new InvalidOperationException("Password reset template not found.");
                }
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
            if (response.User == null)
            {
                throw new InvalidOperationException("User not found for the provided password reset token.");
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
        public async Task<bool> LogoutFromDeviceAsync(int userId, string? sessionId)
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
            SecureString? secureKey = await _keyManagementService.GetCurrentKeyAsync();
            if (secureKey == null)
                throw new InvalidOperationException("No active signing key available.");

            // 2) Convert SecureString to byte[] (this includes base64 decoding).
            byte[] keyBytes;
            nint bstrPtr = Marshal.SecureStringToBSTR(secureKey);
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

            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));


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

        private async Task UpdateLoginAttemptsAndThrowIfLockedAsync(User? user, LoginResponse loginResponse)
        {
            if (_httpContextAccessor.HttpContext == null) return;

            // Use _loginSettings
            int maxAttempts = _loginSettings.MaxFailedAccessAttempts;
            int cookieExpiryMinutes = _loginSettings.CookieExpiryMinutes;
            // bool lockoutEnabled = _loginSettings.LockoutEnabled; // LockoutEnabled is not directly used in the following logic, but MaxFailedAccessAttempts implies it.

            // Read the current attempt count and lockout timestamp from encrypted cookie
            var attemptsCookie = _httpContextAccessor.HttpContext.Request.Cookies[CookieName];
            DateTimeOffset? lockoutExpiry = null;
            int currentAttempts = 0;

            if (!string.IsNullOrEmpty(attemptsCookie))
            {
                try
                {
                    var decryptedValue = _protector.Unprotect(attemptsCookie);
                    var parts = decryptedValue.Split('|');
                    if (parts.Length == 1) // Old format or just attempts
                    {
                        if (int.TryParse(parts[0], out int parsedAttempts))
                            currentAttempts = parsedAttempts;
                    }
                    else if (parts.Length == 2) // Attempts|LockoutExpiry format
                    {
                        if (int.TryParse(parts[0], out int parsedAttempts))
                            currentAttempts = parsedAttempts;
                        if (long.TryParse(parts[1], out long timestampTicks))
                            lockoutExpiry = new DateTimeOffset(timestampTicks, TimeSpan.Zero);
                    }
                }
                catch
                {
                    // If decryption fails or data is tampered, reset.
                    currentAttempts = 0; // Or maxAttempts + 1 to force lockout immediately
                    lockoutExpiry = null;
                }
            }

            // Check if currently locked out
            if (lockoutExpiry.HasValue && DateTimeOffset.UtcNow < lockoutExpiry.Value)
            {
                TimeSpan remainingLockout = lockoutExpiry.Value - DateTimeOffset.UtcNow;
                if (user != null) await user.ClearPHIAsync();
                throw new AccountLockedException($"Account locked. Try again in {Math.Ceiling(remainingLockout.TotalMinutes)} minutes.");
            }

            // If lockout has expired, reset attempts
            if (lockoutExpiry.HasValue && DateTimeOffset.UtcNow >= lockoutExpiry.Value)
            {
                currentAttempts = 0;
                lockoutExpiry = null;
                // Clear the cookie explicitly here or let it be overwritten
                _httpContextAccessor.HttpContext.Response.Cookies.Delete(CookieName);
            }

            // Increment attempts
            currentAttempts++;
            loginResponse.Attempts = currentAttempts; // Update LoginResponse for other potential uses, though not directly used here

            string cookieValueToStore;
            CookieOptions cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Ensure this is true for production
                IsEssential = true,
                // Cookie expiry should be longer than lockout to maintain lockout state
                Expires = DateTime.UtcNow.AddMinutes(Math.Max(cookieExpiryMinutes, _loginSettings.LockoutDurationInMinutes + 5))
            };

            if (currentAttempts > maxAttempts && _loginSettings.LockoutEnabled)
            {
                lockoutExpiry = DateTimeOffset.UtcNow.AddMinutes(_loginSettings.LockoutDurationInMinutes);
                cookieValueToStore = $"{currentAttempts}|{lockoutExpiry.Value.UtcTicks}";
                _logger.LogInformation("Lockout condition: Preparing to protect cookie value: {CookieValue}", cookieValueToStore); // LOGGING

                try
                {
                    var encryptedCookieValue = _protector.Protect(cookieValueToStore);
                    _logger.LogInformation("Lockout condition: Successfully protected cookie value. Encrypted length: {Length}", encryptedCookieValue.Length); // LOGGING

                    _httpContextAccessor.HttpContext.Response.Cookies.Append(CookieName, encryptedCookieValue, cookieOptions);
                    _logger.LogInformation("Lockout condition: Appended lockout cookie to response."); // LOGGING
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lockout condition: CRITICAL - Failed to protect or append lockout cookie. CookieValueToStore: {CookieValueToStore}", cookieValueToStore); // LOGGING
                    // Optionally, rethrow or handle more gracefully if this failure means the lockout cannot be properly communicated
                    // For now, the original AccountLockedException will still be thrown below, but this log is vital.
                }

                TimeSpan currentLockoutDuration = lockoutExpiry.Value - DateTimeOffset.UtcNow;
                if (user != null) await user.ClearPHIAsync();
                throw new AccountLockedException($"Too many login attempts. Account locked for {Math.Ceiling(currentLockoutDuration.TotalMinutes)} minutes.");
            }
            else
            {
                // Store only attempts if not locked out or lockout is disabled
                cookieValueToStore = currentAttempts.ToString();
                // If lockout is not enabled, or not yet exceeding max attempts, cookie lasts for CookieExpiryMinutes
                cookieOptions.Expires = DateTime.UtcNow.AddMinutes(cookieExpiryMinutes);

                // This part also needs to append the cookie
                var encryptedCookieValue = _protector.Protect(cookieValueToStore);
                _httpContextAccessor.HttpContext.Response.Cookies.Append(CookieName, encryptedCookieValue, cookieOptions);
            }

            // This specific assignment to loginResponse.ToMany seems redundant now with exception throwing
            // loginResponse.ToMany = currentAttempts > maxAttempts && _loginSettings.LockoutEnabled;
        }

        // OriginalLoginAttemptsForRefreshAsync method removed

        private void DeleteCookie()
        {
            if (_httpContextAccessor.HttpContext == null) return;

            // If login is successful, remove the cookie to reset attempts
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(CookieName);
        }

        #endregion
    }
}

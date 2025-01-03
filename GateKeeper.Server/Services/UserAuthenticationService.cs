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
using RegisterRequest = GateKeeper.Server.Models.Account.RegisterRequest;
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
        private readonly IEmailService _emailService;
        private readonly IVerifyTokenService _verificationService;
        private readonly IUserService _userService;
        private readonly ISettingsService _settingsService;
        private readonly IKeyManagementService _keyManagementService;

        private const string cookieName = "LoginAttempts";

        /// <summary>
        /// Constructor for UserAuthenticationService.
        /// </summary>
        /// <param name="userService">User-related operations.</param>
        /// <param name="verificationService">Verification token operations.</param>
        /// <param name="configuration">App configuration (used for time-based configs, etc.).</param>
        /// <param name="dbHelper">Database wrapper for DB access.</param>
        /// <param name="logger">Logger for logging information and errors.</param>
        /// <param name="emailService">Service to handle email communications.</param>
        /// <param name="settingsService">Service to retrieve settings for users.</param>
        /// <param name="keyManagementService">Key Management Service for retrieving rotating JWT keys.</param>
        /// <param name="protector"></param>
        /// <param name="httpContextAccessor"></param>
        public UserAuthenticationService(
            IUserService userService,
            IVerifyTokenService verificationService,
            IConfiguration configuration,
            IDbHelper dbHelper,
            ILogger<UserAuthenticationService> logger,
            IEmailService emailService,
            ISettingsService settingsService,
            IKeyManagementService keyManagementService,
            IDataProtectionProvider protector, 
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _dbHelper = dbHelper;
            _logger = logger;
            _emailService = emailService;
            _verificationService = verificationService;
            _userService = userService;
            _settingsService = settingsService;
            _keyManagementService = keyManagementService;
            _protector = protector.CreateProtector("SecureCookies"); ;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public async Task<RegistrationResponse> RegisterUserAsync(RegisterRequest registerRequest)
        {
            RegistrationResponse response = new RegistrationResponse();

            response.User = new User()
            {
                FirstName = registerRequest.FirstName,
                Email = registerRequest.Email,
                LastName = registerRequest.LastName,
                Password = registerRequest.Password,
                Username = registerRequest.Username,
                Phone = registerRequest.Phone
            };


            if (!await ValidatePasswordStrengthAsync(registerRequest.Password))
            {
                response.FailureReason = "Password does not meet the required complexity.";
                return response;
            }

            response = await _userService.RegisterUser(response.User);

            if (!response.IsSuccessful)
                return response;

            await AssignRoleToUser(response.User.Id, "NewUser");
            string results = await _verificationService.GenerateTokenAsync(response.User.Id, "NewUser");

            string emailBody = await File.ReadAllTextAsync("Documents/EmailVerificationTemplate.html");
            emailBody = emailBody.Replace("UNIQUE_VERIFICATION_TOKEN", WebUtility.UrlEncode(results));
            emailBody = emailBody.Replace("FIRST_NAME", response.User.FirstName);
            emailBody = emailBody.Replace("LAST_NAME", response.User.LastName);
            emailBody = emailBody.Replace("EMAIL", response.User.Email);
            emailBody = emailBody.Replace("USERNAME", response.User.Username);
            emailBody = emailBody.Replace("REPLACE_URL", registerRequest.Website);

            await _emailService.SendEmailAsync(response.User.Email, "Your verification token", emailBody);

            return response;
        }

        /// <inheritdoc />
        public async Task<LoginResponse> LoginAsync(UserLoginRequest userLogin)
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
            response.SessionId = response.RefreshToken.Split('.')[0];
            response.IsSuccessful = true;
            response.Settings = userSettings;
            await DeleteCookie();
            return response;
        }

        /// <inheritdoc />
        public async Task<int> LogoutAsync(string? token = null, int userId = 0)
        {
            try
            {
                return await _verificationService.RevokeTokensAsync(userId, "Refresh", token);
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
            response.SessionId = response.RefreshToken.Split('.')[0];
            response.IsSuccessful = true;
            response.Settings = userSettings;
            await DeleteCookie();
            return response;
        }

        public async Task InitiatePasswordResetAsync(User user, InitiatePasswordResetRequest initiateRequest)
        {
            try
            {


                string token = await _verificationService.GenerateTokenAsync(user.Id, "ForgotPassword");

                string emailBody = await File.ReadAllTextAsync("Documents/EmailPasswordChange.html");
                emailBody = emailBody.Replace("UNIQUE_VERIFICATION_TOKEN", WebUtility.UrlEncode(token));
                emailBody = emailBody.Replace("FIRST_NAME", user.FirstName);
                emailBody = emailBody.Replace("LAST_NAME", user.LastName);
                emailBody = emailBody.Replace("EMAIL", user.Email);
                emailBody = emailBody.Replace("USERNAME", user.Username);
                emailBody = emailBody.Replace("REPLACE_URL", initiateRequest.Website);

                await _emailService.SendEmailAsync(user.Email, "Password Reset", emailBody);
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
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            // Get password strength criteria from appsettings.json
            var minLength = Convert.ToInt32(_configuration["PasswordStrength:MinLength"]);
            var requireUppercase = Convert.ToBoolean(_configuration["PasswordStrength:RequireUppercase"]);
            var requireLowercase = Convert.ToBoolean(_configuration["PasswordStrength:RequireLowercase"]);
            var requireDigit = Convert.ToBoolean(_configuration["PasswordStrength:RequireDigit"]);
            var requireSpecialChar = Convert.ToBoolean(_configuration["PasswordStrength:RequireSpecialChar"]);
            var specialChars = _configuration["PasswordStrength:SpecialChars"];

            // Check length
            if (password.Length < minLength)
            {
                return false;
            }

            // Check for uppercase
            if (requireUppercase && !Regex.IsMatch(password, "[A-Z]"))
            {
                return false;
            }

            // Check for lowercase
            if (requireLowercase && !Regex.IsMatch(password, "[a-z]"))
            {
                return false;
            }

            // Check for digit
            if (requireDigit && !Regex.IsMatch(password, "[0-9]"))
            {
                return false;
            }

            // Simplified Special Character Check:
            if (requireSpecialChar && !password.Any(specialChars.Contains))
            {
                return false;
            }

            return await Task.FromResult(true);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SessionInfo>> ManageActiveSessionsAsync(int userId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<bool> LogoutFromDeviceAsync(int userId, string? sessionId = null)
        {
            // Implementation for logging out from a specific device or all devices
            throw new NotImplementedException();
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
        /// Generates a secure refresh token.
        /// </summary>
        /// <returns>Refresh token as a string.</returns>
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }


        /// <summary>
        /// Stores the refresh token in the database.
        /// </summary>
        /// <param name="userId">User ID associated with the token.</param>
        /// <param name="refreshToken">Refresh token to store.</param>
        /// <param name="connection">Active database connection.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<string> StoreVerifyToken(int userId, string verifyToken, string veryfyType, IMySqlConnectorWrapper connection)
        {
            // Generate Refresh Token
            var salt = PasswordHelper.GenerateSalt();
            var hashedVerifyToken = PasswordHelper.HashPassword(verifyToken, salt);
            var tokenId = Guid.NewGuid().ToString(); // Unique identifier for the refresh token

            // Store Refresh Token in DB
            await connection.ExecuteNonQueryAsync("VerificationInsert", CommandType.StoredProcedure,
                new MySqlParameter("@p_Id", tokenId),
                new MySqlParameter("@p_VerifyType", veryfyType),
                new MySqlParameter("@p_UserId", userId),
                new MySqlParameter("@p_HashedToken", hashedVerifyToken),
                new MySqlParameter("@p_Salt", salt),
                new MySqlParameter("@p_ExpiryDate", DateTime.UtcNow.AddDays(7))); // 7-day expiration

            return $"{tokenId}.{verifyToken}";
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

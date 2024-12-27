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

namespace GateKeeper.Server.Services
{
    /// <summary>
    /// Service handling user authentication and related operations.
    /// </summary>
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly IDBHelper _dbHelper;
        private readonly ILogger<UserAuthenticationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IVerifyTokenService _verificationService;
        private readonly IUserService _userService;
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// Constructor for UserAuthenticationService.
        /// </summary>
        /// <param name="configuration">Application configuration dependency.</param>
        /// <param name="dbHelper">Database helper for DB operations.</param>
        /// <param name="logger">Logger for logging information and errors.</param>
        public UserAuthenticationService(IUserService userService, IVerifyTokenService verificationService, IConfiguration configuration, IDBHelper dbHelper, ILogger<UserAuthenticationService> logger, IEmailService emailService, ISettingsService settingsService)
        {
            _configuration = configuration;
            _dbHelper = dbHelper;
            _logger = logger;
            _emailService = emailService;
            _verificationService = verificationService;
            _userService = userService;
            _settingsService = settingsService;
        }

        /// <inheritdoc />
        public async Task RegisterUserAsync(RegisterRequest registerRequest)
        {
            try
            {
                if (!await ValidatePasswordStrengthAsync(registerRequest.Password))
                {
                    throw new ApplicationException("Weak password.");
                }

                var (resultCode, userFinal) = await _userService.AddUser(new User()
                {
                        FirstName = registerRequest.FirstName,
                        Email = registerRequest.Email,
                        LastName = registerRequest.LastName,
                        Password = registerRequest.Password,
                        Username = registerRequest.Username,
                        Phone = registerRequest.Phone});

                
                switch (resultCode)
                {
                    case 1:
                        throw new ApplicationException("Email already exists.");
                    case 2:
                        throw new ApplicationException("Username already exists.");
                    case 3:
                        throw new ApplicationException("Both Email and Username already exist.");
                }

                await AssignRoleToUser(userFinal.Id, "NewUser");
                string results = await _verificationService.GenerateTokenAsync(userFinal.Id, "NewUser");
                
                string emailBody = await File.ReadAllTextAsync("Documents/EmailVerificationTemplate.html"); 
                emailBody = emailBody.Replace("UNIQUE_VERIFICATION_TOKEN", WebUtility.UrlEncode(results));
                emailBody = emailBody.Replace("FIRST_NAME", userFinal.FirstName);
                emailBody = emailBody.Replace("LAST_NAME", userFinal.LastName);
                emailBody = emailBody.Replace("EMAIL", userFinal.Email);
                emailBody = emailBody.Replace("USERNAME", userFinal.Username);
                emailBody = emailBody.Replace("REPLACE_URL", registerRequest.Website);

                await _emailService.SendEmailAsync("skidz@r-u.me", "Your verification token", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<(bool isAuthenticated, string accessToken, string refreshToken, User? user, List<Setting> settings)> LoginAsync(UserLoginRequest userLogin)
        {
            try
            {
                
                User user = await _userService.GetUser(userLogin.Identifier);
                
                int? userId = user?.Id ?? null;
                List<Setting> theSettings = await _settingsService.GetAllSettingsAsync(userId);
                List<Setting> settings = theSettings.Where(s => s.UserId == null).ToList();
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



                if (user == null || string.IsNullOrEmpty(user.Salt) || string.IsNullOrEmpty(user.Password))
                    return (false, "", "", null, settings);

                var hashedPassword = PasswordHelper.HashPassword(userLogin.Password, user.Salt);

                if (hashedPassword != user.Password)
                    return (false, string.Empty, string.Empty, null, settings);

                // Generate tokens
                var accessToken = await GenerateJwtToken(user);
                string refreshToken = await _verificationService.GenerateTokenAsync(user.Id, "Refresh");

                return (true, accessToken, refreshToken, user, userSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login.");
                return (false, string.Empty, string.Empty, null, null);
            }
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
        public async Task<(bool, User?, string)> VerifyNewUser(string verificationCode)
        {
            var tokenId = verificationCode.Split('.')[0];
            User? user = null;
            try
            {
                var (isValid, userTemp, validationType) = 
                    await _verificationService.VerifyTokenAsync(new VerifyTokenRequest()
                    {
                        VerificationCode = verificationCode,
                        TokenType = "NewUser",
                    });
                user = userTemp;

                if (isValid && user != null && validationType == "NewUser")
                {
                    var connection = await _dbHelper.GetOpenConnectionAsync();
                    await using var cmd2 = new MySqlCommand("ValidateFinish", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd2.Parameters.AddWithValue("@p_UserId", user.Id);
                    cmd2.Parameters.AddWithValue("@p_Id", tokenId);
                    await cmd2.ExecuteNonQueryAsync();
                }

                return (isValid, user, validationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<(bool isSuccessful, string accessToken, string refreshToken, User? user, List<Setting> settings)> RefreshTokensAsync(string refreshToken)
        {
            try
            {
                var (authenticated, user, verifyType) = await _verificationService.VerifyTokenAsync(new VerifyTokenRequest()
                {
                    TokenType = "Refresh",
                    VerificationCode = refreshToken
                });

                int? userId = user?.Id ?? null;
                List<Setting> theSettings = await _settingsService.GetAllSettingsAsync(userId);
                List<Setting> settings = theSettings.Where(s => s.UserId == null).ToList();
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

                if (!authenticated || user == null || verifyType != "Refresh")
                    return (false, string.Empty, string.Empty, null, settings);

                // Generate new tokens
                var newAccessToken = await GenerateJwtToken(user);
                string newRefreshToken = await _verificationService.GenerateTokenAsync(user.Id, "Refresh");

                await _verificationService.RevokeTokensAsync(user.Id, "Refresh", refreshToken);
                
                return (true, newAccessToken, newRefreshToken, user, userSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing tokens.");
                return (false, string.Empty, string.Empty, null, new List<Setting>());
            }
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
        public async Task<bool> ResetPasswordAsync(PasswordResetRequest resetRequest)
        {
            var (isValid, userTemp, validationType) =
                await _verificationService.VerifyTokenAsync(new VerifyTokenRequest()
                {
                    TokenType = "ForgotPassword",
                    VerificationCode = resetRequest.ResetToken
                });
            if (userTemp != null && isValid && validationType == "ForgotPassword")
                return (await _userService.ChangePassword(userTemp.Id, resetRequest.NewPassword)) > 0;
             
            return false;
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
            var minLength = _configuration.GetValue<int>("PasswordStrength:MinLength", 8); // Default to 8 if not specified
            var requireUppercase = _configuration.GetValue<bool>("PasswordStrength:RequireUppercase", true);
            var requireLowercase = _configuration.GetValue<bool>("PasswordStrength:RequireLowercase", true);
            var requireDigit = _configuration.GetValue<bool>("PasswordStrength:RequireDigit", true);
            var requireSpecialChar = _configuration.GetValue<bool>("PasswordStrength:RequireSpecialChar", true);
            var specialChars = _configuration.GetValue<string>("PasswordStrength:SpecialChars", "!@#$%^&*()_-+=[{]};:'\",.<>/?`~");

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
        /// Generates a JWT access token for the authenticated user.
        /// </summary>
        /// <param name="user">Authenticated user details.</param>
        /// <returns>JWT access token as a string.</returns>
        private async Task<string> GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtConfig:Secret"]);

            if (user.Roles.Count == 0)
                user.Roles = await _userService.GetRolesAsync(user.Id); // Assume `user.Roles` is a list of roles or groups like ["Admin", "Manager"]
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
            };

            // Add roles as separate claims
            foreach (var role in user.Roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtConfig:ExpirationMinutes"])),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["JwtConfig:Issuer"],
                Audience = _configuration["JwtConfig:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
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
        private async Task<string> StoreVerifyToken(int userId, string verifyToken, string veryfyType, MySqlConnection connection)
        {
            // Generate Refresh Token
            var salt = PasswordHelper.GenerateSalt();
            var hashedVerifyToken = PasswordHelper.HashPassword(verifyToken, salt);
            var tokenId = Guid.NewGuid().ToString(); // Unique identifier for the refresh token

            // Store Refresh Token in DB
            await using (var cmd = new MySqlCommand("VerificationInsert", connection))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_Id", tokenId);
                cmd.Parameters.AddWithValue("@p_VerifyType", veryfyType);
                cmd.Parameters.AddWithValue("@p_UserId", userId);
                cmd.Parameters.AddWithValue("@p_HashedToken", hashedVerifyToken);
                cmd.Parameters.AddWithValue("@p_Salt", salt);
                cmd.Parameters.AddWithValue("@p_ExpiryDate", DateTime.UtcNow.AddDays(7)); // 7-day expiration
                // Add additional parameters as needed
                await cmd.ExecuteNonQueryAsync();
            }

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
            var connection = await _dbHelper.GetOpenConnectionAsync();
            await using var roleCmd = new MySqlCommand("AssignRoleToUser", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            roleCmd.Parameters.AddWithValue("@p_UserId", userId);
            roleCmd.Parameters.AddWithValue("@p_RoleName", role);
            await roleCmd.ExecuteNonQueryAsync();
        }



        


        #endregion
    }
}

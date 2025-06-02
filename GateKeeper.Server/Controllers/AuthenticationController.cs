using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GateKeeper.Server.Extension;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Account.Login;
using GateKeeper.Server.Models.Account.UserModels;
using System.Runtime.CompilerServices;
using GateKeeper.Server.Inherites;
using Microsoft.Extensions.Options; // Added for IOptions
using GateKeeper.Server.Models.Configuration; // Added for RegisterSettingsConfig
using GateKeeper.Server.Exceptions;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// Controller handling authentication and user management operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : MyControllerBase
    {
        private readonly IUserAuthenticationService _authService;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IUserService _userService;
        private readonly IOptions<RegisterSettingsConfig> _registerSettingsOptions; // Added
        private readonly bool _requiresInvite;

        // Constants for response and error messages
        private const string UserRegisteredSuccessfully = "User registered successfully";
        private const string UserVerificationSuccessful = "User verification successful";
        private const string InvalidVerificationCode = "Invalid verification code";
        private const string ExceededLoginAttempts = "You have exceeded the maximum number of login attempts. Please try again after 30 minutes.";
        private const string InvalidCredentials = "Invalid credentials";
        private const string InternalError = "Internal error";
        private const string TokenNotFound = "Token not found";
        private const string TokensRevokedSuccessfully = "{0} token(s) revoked successfully";
        private const string PasswordResetInitiated = "A password reset link has been dispatched. Peek at your inbox (or spam folder, just in case) to continue the mission!";
        private const string PasswordResetError = "An error occurred while resetting password";
        private const string PasswordResetSuccessful = "Password has been reset successfully";
        private const string InvalidRefreshToken = "Invalid Refresh Token";
        private const string FailedToLogoutDevice = "Failed to logout from the specified device";
        private const string LoggedOutSuccessfully = "Logged out successfully";

        /// <summary>
        /// Constructor for AuthenticationController.
        /// </summary>
        /// <param name="authService">Authentication service dependency.</param>
        /// <param name="logger">Logger for logging information and errors.</param>
        /// <param name="userService">User service dependency.</param>
        public AuthenticationController(
            IUserAuthenticationService authService,
            ILogger<AuthenticationController> logger,
            IUserService userService,
            IOptions<RegisterSettingsConfig> registerSettingsOptions) // Modified parameters
        {
            _authService = authService;
            _logger = logger;
            _userService = userService;
            _registerSettingsOptions = registerSettingsOptions; // Assigned new field
            _requiresInvite = _registerSettingsOptions.Value.RequireInvite; // Updated assignment
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="registerRequest">User registration details.</param>
        /// <returns>Action result indicating the outcome of the registration.</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();

            try
            {
                var registrationResponse = await _authService.RegisterUserAsync(registerRequest);

                // If we reach here, registration was successful based on service layer not throwing
                _logger.LogInformation("User registered: {UserId}, IP: {IpAddress}, Device: {UserAgent}",
                    registrationResponse.User?.Id, userIp, userAgent);

                if (registerRequest.UserLicAgreement) // This logging can stay if needed
                {
                    _logger.LogInformation("User agreed to the User License Agreement: {UserId}, IP: {IpAddress}",
                        registrationResponse.User?.Id, userIp);
                }

                return StatusCode(201, new { message = UserRegisteredSuccessfully });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during user registration. IP: {IpAddress}, UserAgent: {UserAgent}. Email: {Email}, Username: {Username}", userIp, userAgent, registerRequest.Email.SanitizeForLogging(), registerRequest.Username.SanitizeForLogging());
                throw; // Re-throw for the global exception handler
            }
        }

        /// <summary>
        /// Verifies a new user using a verification code.
        /// </summary>
        /// <param name="request">Verification request containing the verification code.</param>
        /// <returns>Action result indicating the outcome of the verification.</returns>
        [HttpPost("verify-user")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyNewUser([FromBody] VerifyUserRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();
            TokenVerificationResponse verificationResponse = null;
            try
            {
                verificationResponse = await _authService.VerifyNewUser(request.VerificationCode);
                // If here, verification was successful
                _logger.LogInformation("User verification successful: {UserId}, IP: {IpAddress}, Device: {UserAgent}",
                    verificationResponse.User?.Id, userIp, userAgent);
                return Ok(new { message = UserVerificationSuccessful });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during user verification. IP: {IpAddress}, UserAgent: {UserAgent}, Code: {VerificationCode}", userIp, userAgent, request.VerificationCode.SanitizeForLogging());
                throw;
            }
        }

        /// <summary>
        /// Authenticates a user and issues JWT tokens.
        /// </summary>
        /// <param name="loginRequest">User login credentials.</param>
        /// <returns>Action result containing tokens and user information if successful.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest loginRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();

            try
            {
                var loginResponse = await _authService.LoginAsync(loginRequest, userIp, userAgent);

                _logger.LogInformation("User login successful: {UserId}, IP: {IpAddress}, Device: {UserAgent}",
                    loginResponse.User?.Id, userIp, userAgent);

                return Ok(new
                {
                    loginResponse.AccessToken,
                    loginResponse.RefreshToken,
                    loginResponse.User,
                    loginResponse.Settings,
                    loginResponse.SessionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during login for identifier {Identifier}. IP: {IpAddress}. UserAgent: {UserAgent}", loginRequest.Identifier.SanitizeForLogging(), userIp, userAgent);
                throw;
            }
        }

        /// <summary>
        /// Refreshes JWT tokens using a valid refresh token.
        /// </summary>
        /// <param name="refreshRequest">Refresh token request.</param>
        /// <returns>Action result containing new tokens if successful.</returns>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();
            try
            {
                var loginResponse = await _authService.RefreshTokensAsync(refreshRequest.RefreshToken);

                _logger.LogInformation("User refreshed token successful: {UserId}, IP: {IpAddress}, Device: {UserAgent}",
                    loginResponse.User?.Id, userIp, userAgent);

                return Ok(new
                {
                    accessToken = loginResponse.AccessToken,
                    refreshToken = loginResponse.RefreshToken,
                    user = loginResponse.User,
                    settings = loginResponse.Settings,
                    loginResponse.SessionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during token refresh. IP: {IpAddress}, UserAgent: {UserAgent}", userIp, userAgent);
                throw;
            }
        }

        /// <summary>
        /// Logs out a user by revoking tokens.
        /// </summary>
        /// <param name="logoutRequest">Logout request containing optional token and user ID.</param>
        /// <returns>Action result indicating the outcome of the logout.</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest logoutRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = 0; // Initialize userId
            var revokedCount = 0;
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""; // Define userIp here to be accessible in catch
            try
            {
                userId = GetUserIdFromClaims();
                revokedCount = await _authService.LogoutAsync(logoutRequest.Token, userId);
                return Ok(new { message = string.Format(TokensRevokedSuccessfully, revokedCount) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during logout. UserId: {UserId}, IP: {IpAddress}, Token: {Token}", userId, userIp, logoutRequest.Token.SanitizeForLogging());
                throw;
            }
            finally
            {
                // userIp is already defined above. For safety, can recapture or ensure it's passed if needed.
                // string userIpFinal = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                if (revokedCount > 0)
                {
                    _logger.LogInformation("User logout successful: {UserId}, IP: {IpAddress}, RevokedCount: {RevokedCount}", userId, userIp, revokedCount);
                }
                else
                {
                    _logger.LogWarning("User logout attempt completed with no tokens revoked. UserId: {UserId}, IP: {IpAddress}, Token: {Token}", userId, userIp, logoutRequest.Token.SanitizeForLogging());
                }
            }
        }

        /// <summary>
        /// Initiates the password reset process by sending a reset link or code.
        /// </summary>
        /// <param name="initiateRequest">Password reset initiation request.</param>
        /// <returns>Action result indicating the outcome of the initiation.</returns>
        [HttpPost("password-reset/initiate")]
        [AllowAnonymous]
        public IActionResult InitiatePasswordReset([FromBody] InitiatePasswordResetRequest initiateRequest)
        {
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();
            string failureReason = "";
            var userId = 0;
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                User? user = _userService.GetUser(initiateRequest.EmailOrUsername).Result;
                userId = user?.Id ?? 0;
                if (user == null) return BadRequest("Unknown Username or Email");

                // Fire and forget the background process
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _authService.InitiatePasswordResetAsync(user, initiateRequest);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "User initiated password reset error:: {UserId}, Identifier: {Identifier}, IP: {IpAddress}, Device: {UserAgent}",
                            userId, initiateRequest.EmailOrUsername.SanitizeForLogging(), userIp, userAgent);
                    }
                });

            }
            catch (Exception ex) // This outer catch is being removed
            {
                failureReason = $"{InternalError}: {ex.Message}";
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
            finally
            {
                if (failureReason.Length == 0)
                    _logger.LogInformation("User initiates password reset: {UserId}, Identifier: {Identifier}, IP: {IpAddress}, Device: {UserAgent}",
                        userId, initiateRequest.EmailOrUsername.SanitizeForLogging(), userIp, userAgent);
                else
                    _logger.LogInformation("Error while User initiates password reset: {UserId}, Identifier: {Identifier}, IP: {IpAddress}, Device: {UserAgent}, Reason: {Reason}",
                        userId, initiateRequest.EmailOrUsername.SanitizeForLogging(), userIp, userAgent, failureReason);
            }
            return Ok(new { message = PasswordResetInitiated });
        }

        /// <summary>
        /// Resets the user's password using a reset token or security answers.
        /// </summary>
        /// <param name="resetRequest">Password reset request containing token and new password.</param>
        /// <returns>Action result indicating the outcome of the password reset.</returns>
        [HttpPost("password-reset/reset")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetRequest resetRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();
            TokenVerificationResponse verificationResponse = null;
            try
            {
                verificationResponse = await _authService.ResetPasswordAsync(resetRequest);
                 _logger.LogInformation("Password changed for {UserId}, IP: {IpAddress}, Method: {Method}, Device: {Device}",
                    verificationResponse.User?.Id, userIp, "Token verification", userAgent);
                return Ok(new { message = PasswordResetSuccessful });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during password reset. IP: {IpAddress}, UserAgent: {UserAgent}", userIp, userAgent);
                throw;
            }
        }

        /// <summary>
        /// Checks if a username is available.
        /// </summary>
        /// <param name="username">Username to check.</param>
        /// <returns>Action result indicating if the username can be used.</returns>
        [HttpGet("check-username")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckUsername(string username)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var isValid = !await _authService.UsernameExistsAsync(username);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        /// <summary>
        /// Checks if an email is available.
        /// </summary>
        /// <param name="email">Email to check.</param>
        /// <returns>Action result indicating if the email can be used.</returns>
        [HttpGet("check-email")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var isValid = !await _authService.EmailExistsAsync(email);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{FunctionName}() - Error: {ErrorMessage}",
                    FunctionName(), ex.Message);
                return StatusCode(500, new { error = InternalError });
            }
        }

        /// <summary>
        /// Validates the strength of a provided password.
        /// </summary>
        /// <param name="validateRequest">Password validation request.</param>
        /// <returns>Action result indicating whether the password is strong.</returns>
        [HttpPost("validate-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidatePasswordStrength([FromBody] ValidatePasswordRequest validateRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var isValid = await _authService.ValidatePasswordStrengthAsync(validateRequest.Password);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{FunctionName}() - Error: {ErrorMessage}",
                    FunctionName(), ex.Message);
                return StatusCode(500, new { error = InternalError });
            }
        }

        /// <summary>
        /// Logs out the user from a specific device or all devices.
        /// </summary>
        /// <param name="logoutDeviceRequest">Logout device request containing optional session ID.</param>
        /// <returns>Action result indicating the outcome of the logout operation.</returns>
        [HttpPost("logout-device")]
        [Authorize]
        public async Task<IActionResult> LogoutFromDevice([FromBody] LogoutDeviceRequest logoutDeviceRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var userId = GetUserIdFromClaims();
                var isLoggedOut = await _authService.LogoutFromDeviceAsync(userId, logoutDeviceRequest.SessionId);
                if (isLoggedOut)
                {
                    return Ok(new { message = LoggedOutSuccessfully });
                }
                else
                {
                    return BadRequest(new { error = FailedToLogoutDevice });
                }
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        

        #region Private Functions

        private int GetUserIdFromClaims()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
        #endregion
    }
}

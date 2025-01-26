using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GateKeeper.Server.Extension;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Models.Account.Login;
using GateKeeper.Server.Models.Account.UserModels;
using System.Runtime.CompilerServices;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// Controller handling authentication and user management operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserAuthenticationService _authService;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
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
            IConfiguration configuration)
        {
            _authService = authService;
            _logger = logger;
            _userService = userService;
            _configuration = configuration;
            _requiresInvite = configuration.GetValue<bool>("RegisterSettings:RequireInvite");
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
            RegistrationResponse response = new();
            if (!ModelState.IsValid) return BadRequest(ModelState);
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();

            try
            {
                response = await _authService.RegisterUserAsync(registerRequest);
                if (response.IsSuccessful)
                    return StatusCode(201, new
                    {
                        message = UserRegisteredSuccessfully
                    });

                if (registerRequest.UserLicAgreement)
                    _logger.LogInformation("User agreed to the User License Agreement: {UserId}, Proof: {Proof}, IP: {IpAddress}", response.User?.Id, userIp, registerRequest.UserLicAgreement);

                return BadRequest(new { error = response.FailureReason });
            }
            catch (Exception ex)
            {
                response.FailureReason = $"{InternalError}: {ex.Message}";
                response.IsSuccessful = false;
                return HandleInternalError(CurrentFunctionName(), ex);
            }
            finally
            {
                if (response.IsSuccessful)
                {
                    _logger.LogInformation("User registered: {UserId}, IP: {IpAddress}, Device: {UserAgent}",
                        response.User?.Id, userIp, userAgent);
                }
                else
                {
                    _logger.LogWarning("User registration failed {UserId}, IP: {IpAddress}, Reason: {Reason}",
                        response.User?.Id, userIp, response.FailureReason);
                }
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
            TokenVerificationResponse response = new();
            try
            {
                response = await _authService.VerifyNewUser(request.VerificationCode);
                if (response.IsVerified)
                    return Ok(new { message = UserVerificationSuccessful });
                return BadRequest(new { error = InvalidVerificationCode });
            }
            catch (Exception ex)
            {
                response.IsVerified = false;
                response.FailureReason = $"{InternalError}: {ex.Message}";
                if (response.User != null)
                    await response.User.ClearPHIAsync();
                return HandleInternalError(CurrentFunctionName(), ex);
            }
            finally
            {
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();
                if (response.IsVerified)
                {
                    _logger.LogInformation("User verification: {UserId}, IP: {IpAddress}, Device: {UserAgent}",
                        response.User?.Id, userIp, userAgent);
                }
                else
                {
                    _logger.LogWarning("User verification failed {UserId}, IP: {IpAddress}, Reason: {Reason}, Token: {Token}",
                        response.User?.Id, userIp, response.FailureReason, response.VerificationCode.SanitizeForLogging());
                }
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
            LoginResponse response = new();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();

            try
            {
                // Attempt to authenticate
                response = await _authService.LoginAsync(loginRequest, userIp, userAgent);

                if (!response.IsSuccessful || response.User == null)
                {
                    return Unauthorized(response.ToMany ?
                        new { error = ExceededLoginAttempts } :
                        new { error = InvalidCredentials });
                }

                // Return the tokens and user info
                return Ok(new
                {
                    response.AccessToken,
                    response.RefreshToken,
                    response.User,
                    response.Settings,
                    response.SessionId
                });
            }
            catch (Exception ex)
            {
                response.IsSuccessful = false;
                response.FailureReason = $"{InternalError}: {ex.Message}";
                return HandleInternalError(CurrentFunctionName(), ex);
            }
            finally
            {
                if (response.IsSuccessful)
                {
                    _logger.LogInformation("User login successful: {UserId}, IP: {IpAddress}, Device: {UserAgent}",
                        response.User?.Id, userIp, userAgent);
                }
                else
                {
                    _logger.LogWarning("User login failed for {UserId}, IP: {IpAddress}, Reason: {Reason}",
                        response.User?.Id, userIp, response.FailureReason);
                }
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
            LoginResponse response = new();
            try
            {
                response = await _authService.RefreshTokensAsync(refreshRequest.RefreshToken);
                if (!response.IsSuccessful)
                    return Unauthorized(response.ToMany ?
                        new { error = ExceededLoginAttempts } :
                        new { error = InvalidRefreshToken });

                return Ok(new
                {
                    accessToken = response.AccessToken,
                    refreshToken = response.RefreshToken,
                    user = response.User,
                    settings = response.Settings,
                    response.SessionId
                });
            }
            catch (Exception ex)
            {
                response.IsSuccessful = false;
                response.FailureReason = $"{InternalError}: {ex.Message}";
                return HandleInternalError(CurrentFunctionName(), ex);
            }
            finally
            {
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();
                if (response.IsSuccessful)
                    _logger.LogInformation("User refreshed token successful: {UserId}, IP: {IpAddress}, Device: {UserAgent}",
                        response.User?.Id, userIp, userAgent);
                else
                    _logger.LogWarning("User refreshed token failed for {UserId}, IP: {IpAddress}, Reason: {Reason}",
                        response.User?.Id, userIp, response.FailureReason);
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
            var userId = 0;
            var revokedCount = 0;
            string failureReason = "";
            try
            {
                userId = GetUserIdFromClaims();
                revokedCount = await _authService.LogoutAsync(logoutRequest.Token, userId);
                if (revokedCount == 0)
                    failureReason = TokenNotFound;
                return Ok(new { message = string.Format(TokensRevokedSuccessfully, revokedCount) });
            }
            catch (Exception ex)
            {
                revokedCount = 0;
                failureReason = $"{InternalError}: {ex.Message}";
                return HandleInternalError(CurrentFunctionName(), ex);
            }
            finally
            {
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                if (revokedCount > 0)
                    _logger.LogInformation("User logout: {UserId}, IP: {IpAddress}",
                        userId, userIp);
                else
                    _logger.LogWarning("User logout with unknown token: {UserId}, IP: {IpAddress}, token:{RefreshToken}, Reason: {Reason}",
                        userId, userIp, logoutRequest.Token.SanitizeForLogging(), failureReason);
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
            catch (Exception ex)
            {
                failureReason = $"{InternalError}: {ex.Message}";
                return HandleInternalError(CurrentFunctionName(), ex);
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
            TokenVerificationResponse response = new();
            try
            {
                response = await _authService.ResetPasswordAsync(resetRequest);
                if (response.IsVerified)
                    return Ok(new { message = PasswordResetSuccessful });
                else
                    return BadRequest(new { message = PasswordResetError });
            }
            catch (Exception ex)
            {
                response.FailureReason = $"{InternalError}: {ex.Message}";
                response.IsVerified = false;
                response.User?.ClearPHIAsync();
                return HandleInternalError(CurrentFunctionName(), ex);
            }
            finally
            {
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();
                if (response.IsVerified)
                    _logger.LogInformation("Password changed for {UserId}, IP: {IpAddress}, Method: {Method}, Device: {Device}",
                        response.User?.Id, userIp, "Token verification", userAgent);
                else
                    _logger.LogWarning("Password reset failed for {UserId}, IP: {IpAddress}, Method: {Method}, Reason: {Reason}",
                        response.User?.Id, userIp, "Token verification", response.FailureReason);
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
                return HandleInternalError(CurrentFunctionName(), ex);
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
                return HandleInternalError(CurrentFunctionName(), ex);
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
                return HandleInternalError(CurrentFunctionName(), ex);
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
                return HandleInternalError(CurrentFunctionName(), ex);
            }
        }

        /// <summary>
        /// Checks if the registration requires an invitation.
        /// </summary>
        /// <returns>Action result indicating if the registration requires an invitation.</returns>
        [HttpGet("is-invite-only")]
        [AllowAnonymous]
        public IActionResult IsInviteOnly()
        {
            try
            {
                return Ok(new { _requiresInvite });
            }
            catch (Exception ex)
            {
                return HandleInternalError(CurrentFunctionName(), ex);
            }
        }

        #region Private Functions

        private int GetUserIdFromClaims()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        private IActionResult HandleInternalError(string functionName, Exception ex)
        {
            _logger.LogError(ex, "There was an error with function: {Function} of {ErrorMessage}", functionName, ex.Message);
            return StatusCode(500, new { error = InternalError });
        }

        private static string CurrentFunctionName([CallerMemberName] string functionName = "")
        {
            return functionName;
        }

        #endregion
    }
}

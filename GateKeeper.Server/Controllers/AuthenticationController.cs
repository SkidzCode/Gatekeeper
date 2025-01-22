using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Resources;
using GateKeeper.Server.Models.Account.Login;
using GateKeeper.Server.Models.Account.UserModels;

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
            _requiresInvite = _configuration.GetValue<bool>("RegisterSettings:RequireInvite");
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
            string userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                response = await _authService.RegisterUserAsync(registerRequest);
                if (response.IsSuccessful)
                    return StatusCode(201, new
                    {
                        message = string.Format(DialogRegister.RegisterSucess, registerRequest.Username,
                            registerRequest.Email)
                    });

                if (registerRequest.UserLicAgreement)
                    _logger.LogInformation("User agreed to the User License Agreement: {UserId}, Proof: {Proof}, IP: {IpAdress}", response.User?.Id, userIp, registerRequest.UserLicAgreement);

                return BadRequest(new { error = response.FailureReason });
            }
            catch (Exception ex)
            {
                response.FailureReason = "Internal Error: " + ex.Message;
                response.IsSuccessful = false;
                return HandleInternalError(ex, DialogRegister.RegisterError);
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
                    return Ok(new { message = DialogRegister.RegisterSucess });
                return BadRequest(new { error = DialogVerify.VerifyInvalid });
            }
            catch (Exception ex)
            {
                response.IsVerified = false;
                response.FailureReason = "Internal error: " + ex.Message;
                if (response.User != null)
                    await response.User.ClearPHIAsync();
                return HandleInternalError(ex, DialogVerify.VerifyError);
            }
            finally
            {
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                string userAgent = Request.Headers["User-Agent"].ToString();
                if (response.IsVerified)
                {
                    _logger.LogInformation("User verification: {UserId}, IP: {IpAddress}, Device: {UserAgent}",
                        response.User?.Id, userIp, userAgent);
                }
                else
                {
                    // response.User could be null if login fails, so we use null-conditional
                    _logger.LogWarning("User verification failed {UserId}, IP: {IpAddress}, Reason: {Reason}, Token: {Token}",
                        response.User?.Id, userIp, response.FailureReason, response.VerificationCode);
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

            try
            {
                // Attempt to authenticate
                response = await _authService.LoginAsync(loginRequest);

                if (!response.IsSuccessful || response.User == null)
                {
                    return Unauthorized(response.ToMany ? 
                        new { error = DialogLogin.LoginMaxAttempts } : 
                        new { error = DialogLogin.LoginInvalid });
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
                response.FailureReason = "Internal error: " + ex.Message;
                return HandleInternalError(ex, DialogLogin.LoginError);
            }
            finally
            {
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                string userAgent = Request.Headers["User-Agent"].ToString();
                if (response.IsSuccessful)
                {
                    _logger.LogInformation("User login successful: {UserId}, IP: {IpAddress}, Device: {UserAgent}",
                        response.User?.Id, userIp, userAgent);
                }
                else
                {
                    // response.User could be null if login fails, so we use null-conditional
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
                        new { error = DialogLogin.LoginMaxAttempts } :
                        new { error = DialogLogin.LoginInvalidRefreshToken }); 

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
                response.FailureReason = "Internal error: " + ex.Message;
                return HandleInternalError(ex, DialogLogin.LoginInvalidRefreshToken);
            }
            finally
            {
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                string userAgent = Request.Headers["User-Agent"].ToString();
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
                    failureReason = "Token not found";
                return Ok(new { message = string.Format(DialogLogin.LogoutRevokeToken, revokedCount) });
            }
            catch (Exception ex)
            {
                revokedCount = 0;
                failureReason = "Internal error: " + ex.Message;
                return HandleInternalError(ex, DialogLogin.LogoutError);
            }
            finally
            {
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                if (revokedCount > 0)
                    _logger.LogInformation("User logout: {UserId}, IP: {IpAddress}",
                        userId, userIp);
                else
                    _logger.LogWarning("User logout with unknown token: {UserId}, IP: {IpAddress}, token:{RefreshToken}, Reason: {Reason}",
                        userId, userIp, logoutRequest.Token, failureReason);
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
            string userAgent = Request.Headers["User-Agent"].ToString();
            string failureReason = "";
            var userId = 0;
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                User? user = _userService.GetUser(initiateRequest.EmailOrUsername).Result;
                userId = user?.Id ?? 0;
                if (user == null) return BadRequest(DialogPassword.UserMissing);

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
                            userId, initiateRequest.EmailOrUsername, userIp, userAgent);
                    }
                });
                
            }
            catch (Exception ex)
            {
                failureReason = "Internal error: " + ex.Message;
                return HandleInternalError(ex, DialogPassword.UserPasswordResetInitiateError);
            }
            finally
            {
                if (failureReason.Length == 0)
                    _logger.LogInformation("User initiates password reset: {UserId}, Identifier: {Identifier}, IP: {IpAddress}, Device: {UserAgent}",
                        userId, initiateRequest.EmailOrUsername, userIp, userAgent);
                else
                    _logger.LogInformation("Error while User initiates password reset: {UserId}, Identifier: {Identifier}, IP: {IpAddress}, Device: {UserAgent}, Reason: {Reason}",
                        userId, initiateRequest.EmailOrUsername, userIp, userAgent, failureReason);
            }
            return Ok(new { message = string.Format(DialogPassword.UserPasswordResetStarted, initiateRequest.EmailOrUsername) });
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
                    return Ok(new { message = DialogPassword.UserPasswordResetSuccess });
                else
                    return BadRequest(new { message = DialogPassword.UserPasswordResetError });
            }
            catch (Exception ex)
            {
                response.FailureReason = "Internal error: " + ex.Message;
                response.IsVerified = false;
                response.User?.ClearPHIAsync();
                return HandleInternalError(ex, DialogPassword.UserPasswordResetTokenError);
            }
            finally
            {
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                string userAgent = Request.Headers["User-Agent"].ToString();
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
                return HandleInternalError(ex, DialogPassword.UserPasswordStrengthError);
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
                return HandleInternalError(ex, DialogRegister.EmailExist);
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
                return HandleInternalError(ex, DialogPassword.UserPasswordStrengthError);
            }
        }

        /// <summary>
        /// Retrieves active sessions for the authenticated user.
        /// </summary>
        /// <returns>Action result containing a list of active sessions.</returns>
        [HttpGet("sessions")]
        [Authorize]
        public async Task<IActionResult> ManageActiveSessions()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                var sessions = await _authService.ManageActiveSessionsAsync(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return HandleInternalError(ex, DialogLogin.SessionGetError);
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
                    return Ok(new { message = DialogLogin.LogoutDeviceSucess });
                }
                else
                {
                    return BadRequest(new { error = DialogLogin.LogoutFailure });
                }
            }
            catch (Exception ex)
            {
                return HandleInternalError(ex, DialogLogin.LogoutDeviceError);
            }
        }

        /// <summary>
        /// Checks if the registration requires an invite.
        /// </summary>
        /// <returns>Action result indicating if the registration requires an invite.</returns>
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
                return HandleInternalError(ex, "An error occurred while checking if registration requires an invite.");
            }
        }




        #region private Functions

        private int GetUserIdFromClaims()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        private IActionResult HandleInternalError(Exception ex, string errorMessageTemplate)
        {
            var errorMessage = string.Format(errorMessageTemplate, ex.Message);
            _logger.LogError(ex, errorMessage);
            return StatusCode(500, new { error = errorMessage });
        }
        #endregion
    }
}

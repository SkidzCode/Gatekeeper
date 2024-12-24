using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Resources;

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

        /// <summary>
        /// Constructor for AuthenticationController.
        /// </summary>
        /// <param name="authService">Authentication service dependency.</param>
        /// <param name="logger">Logger for logging information and errors.</param>
        /// <param name="userService">User service dependency.</param>
        public AuthenticationController(IUserAuthenticationService authService, ILogger<AuthenticationController> logger, IUserService userService)
        {
            _authService = authService;
            _logger = logger;
            _userService = userService;
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
            try
            {
                await _authService.RegisterUserAsync(registerRequest);
                return StatusCode(201, new
                {
                    message =
                    string.Format(DialogRegister.RegisterSucess, registerRequest.Username, registerRequest.Email)
                });
            }
            catch (ApplicationException ex)
            {
                _logger.LogWarning(ex, DialogRegister.RegisterFailure);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return HandleInternalError(ex, DialogRegister.RegisterError);
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
            try
            {
                var (isVerified, _, verificationType) = await _authService.VerifyNewUser(request.VerificationCode);
                if (isVerified && verificationType == "NewUser")
                    return Ok(new { message = DialogRegister.RegisterSucess });
                return BadRequest(new { error = DialogVerify.VerifyInvalid });
            }
            catch (Exception ex)
            {
                return HandleInternalError(ex, DialogVerify.VerifyError);
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
            try
            {
                var (isAuthenticated, accessToken, refreshToken, user) = await _authService.LoginAsync(loginRequest);

                if (!isAuthenticated || user == null)
                    return Unauthorized(new { error = DialogLogin.LoginInvalid });

                return Ok(new
                {
                    accessToken,
                    refreshToken,
                    user = new
                    {
                        user.Id,
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        user.Username,
                        user.Phone
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleInternalError(ex, "DialogLogin.LoginError");
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
            try
            {
                var (isSuccessful, newAccessToken, newRefreshToken, user) =
                    await _authService.RefreshTokensAsync(refreshRequest.RefreshToken);

                if (!isSuccessful)
                    return Unauthorized(new { error = DialogLogin.LoginInvalidRefreshToken });

                return Ok(new
                {
                    accessToken = newAccessToken,
                    refreshToken = newRefreshToken,
                    user = user
                });
            }
            catch (Exception ex)
            {
                return HandleInternalError(ex, DialogLogin.LoginInvalidRefreshToken);
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
            try
            {
                string? nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = int.Parse((User.FindFirst("UserId")?.Value ?? nameIdentifier) ?? string.Empty);

                var revokedCount = await _authService.LogoutAsync(logoutRequest.Token, userId);
                return Ok(new { message = string.Format(DialogLogin.LogoutRevokeToken, revokedCount) });
            }
            catch (Exception ex)
            {
                return HandleInternalError(ex, DialogLogin.LogoutError);
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
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                User? user = _userService.GetUser(initiateRequest.EmailOrUsername).Result;
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
                        _logger.LogError(ex, "An error occurred during the password reset background process.");
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleInternalError(ex, DialogPassword.UserPasswordResetInitiateError);
            }

            // Return response immediately to the user
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
            try
            {
                var result = await _authService.ResetPasswordAsync(resetRequest);
                if (result)
                    return Ok(new { message = DialogPassword.UserPasswordResetSuccess });
                else
                    return BadRequest(new { message = DialogPassword.UserPasswordResetError });
            }
            catch (Exception ex)
            {
                return HandleInternalError(ex, DialogPassword.UserPasswordResetTokenError);
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

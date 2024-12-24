using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Services;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// Controller handling authentication and user management operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VerificationController : ControllerBase
    {
        private readonly IVerifyTokenService _verificationService;
        private readonly ILogger<VerificationController> _logger;

        /// <summary>
        /// Constructor for VerificationController.
        /// </summary>
        /// <param name="authService">Authentication service dependency.</param>
        /// <param name="logger">Logger for logging information and errors.</param>
        public VerificationController(IVerifyTokenService verificationService, ILogger<VerificationController> logger)
        {
            _verificationService = verificationService;
            _logger = logger;
        }

        /// <summary>
        /// Verifies a user's account using a verification code.
        /// </summary>
        /// <param name="verifyRequest">Verification code request.</param>
        /// <returns>Action result indicating the outcome of the verification.</returns>
        [HttpPost("Validate")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateTokenAsync([FromBody] VerifyTokenRequest verifyRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var (isAuthenticated, user, verificationType) = await _verificationService.VerifyTokenAsync(verifyRequest);
                if (!isAuthenticated)
                    return BadRequest("Invalid Token");

                _verificationService.RevokeTokensAsync(user.Id, verifyRequest.TokenType, verifyRequest.VerificationCode);

                return Ok(new { message = "Token Valid" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during validation token validation.");
                return StatusCode(500, new { error = "An error occurred during validation token validation." });
            }
        }

        /// <summary>
        /// Verifies a user's account using a verification code.
        /// </summary>
        /// <param name="verifyRequest">Verification code request.</param>
        /// <returns>Action result indicating the outcome of the verification.</returns>
        [HttpPost("Generate")]
        [AllowAnonymous]
        [Authorize]
        public async Task<IActionResult> GenerateTokenAsync([FromBody] VerifyGenerateTokenRequest verifyRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                string? nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = int.Parse((User.FindFirst("UserId")?.Value ?? nameIdentifier) ?? string.Empty);
                if (userId != verifyRequest.UserId)
                    return BadRequest("Invalid user ID.");
                var tokenResult = await _verificationService.GenerateTokenAsync(verifyRequest.UserId, verifyRequest.TokenType);
                return Ok(new { message = "Token generated successfully.", token = tokenResult });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user verification.");
                return StatusCode(500, new { error = "An error occurred while verifying the user." });
            }
        }
    }


}

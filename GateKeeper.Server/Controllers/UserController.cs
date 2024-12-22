using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using GateKeeper.Server.Models;
using GateKeeper.Server.Services;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Security.Claims;
using GateKeeper.Server.Interface;
using System.IdentityModel.Tokens.Jwt;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Resources;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// API controller for handling user-related operations such as registration, login, token refresh, and profile retrieval.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IDBHelper _dbHelper;
        // Service for handling JSON Web Token (JWT) operations
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// Constructor for the UserController.
        /// </summary>
        /// <param name="configuration">Application configuration dependency.</param>
        /// <param name="jwtService">JWT service dependency.</param>
        public UserController(IConfiguration configuration, IDBHelper dbHelper, IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _dbHelper = dbHelper;
            _logger = logger;
        }

        /// <summary>
        /// API endpoint to revoke tokens (e.g., logout).
        /// </summary>
        /// <param name="token">Optional specific token to revoke, otherwise, all tokens for the user will be revoked.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("Update")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _userService.UpdateUser(user);
                return Ok(new { message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format(DialogPassword.UserPasswordResetTokenError, ex.Message) ?? "";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// API endpoint to retrieve profile information of the authenticated user.
        /// </summary>
        /// <returns>User profile details if successful or error if not.</returns>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                int userId = 0;
                // Retrieve the user's email from JWT claims
                if (!int.TryParse(User.FindFirst("Id")?.Value, out userId) || userId == 0)
                {
                    return Unauthorized("Invalid token");
                }

                User? user = await _userService.GetUser(userId);

                if (user == null)
                    return NotFound("User not found.");

                return Ok(user);
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format(DialogLogin.ProfileLoadError, ex.Message) ?? "";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}
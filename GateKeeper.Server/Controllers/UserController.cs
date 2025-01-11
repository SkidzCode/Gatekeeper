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
using GateKeeper.Server.Models.Account.UserModels;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// API controller for handling user-related operations such as registration, login, token refresh, and profile retrieval.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IDbHelper _dbHelper;
        // Service for handling JSON Web Token (JWT) operations
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// Constructor for the UserController.
        /// </summary>
        /// <param name="configuration">Application configuration dependency.</param>
        /// <param name="jwtService">JWT service dependency.</param>
        public UserController(
            IConfiguration configuration, 
            IDbHelper dbHelper, 
            IUserService userService, 
            ILogger<UserController> logger,
            IRoleService roleService)
        {
            _userService = userService;
            _dbHelper = dbHelper;
            _logger = logger;
            _roleService = roleService;
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

        /// <summary>
        /// API endpoint to retrieve list of users.
        /// </summary>
        /// <returns>List of users</returns>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                int userId = 0;
                List<User?> users = await _userService.GetUsers();

                return Ok(users);
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format(DialogLogin.ProfileLoadError, ex.Message) ?? "";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// API endpoint to retrieve list of users.
        /// </summary>
        /// <returns>List of users</returns>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers(int userId)
        {
            try
            {
                User? user = await _userService.GetUser(userId);
                return Ok(user);
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format(DialogLogin.ProfileLoadError, ex.Message) ?? "";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// API endpoint to retrieve list of users.
        /// </summary>
        /// <returns>List of users</returns>
        [HttpGet("user/edit/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsersEdit(int userId)
        {
            try
            {
                User? user = await _userService.GetUser(userId);
                List<Role> roles = await _roleService.GetAllRoles();
                return Ok(new { user, roles });
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format(DialogLogin.ProfileLoadError, ex.Message) ?? "";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        // Existing dependencies and constructors...

        [HttpPost("UpdateWithImage")]
        [Authorize]
        public async Task<IActionResult> UpdateUserWithImage([FromForm] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Map the DTO to your User model
                var user = new User
                {
                    Id = updateUserDto.Id,
                    FirstName = updateUserDto.FirstName,
                    LastName = updateUserDto.LastName,
                    Email = updateUserDto.Email,
                    Username = updateUserDto.Username,
                    Phone = updateUserDto.Phone
                    // Other properties as needed
                };

                if (updateUserDto.ProfilePicture != null)
                {
                    // Validate the image
                    var validImageTypes = new[] { "image/jpeg", "image/png" };
                    if (!validImageTypes.Contains(updateUserDto.ProfilePicture.ContentType))
                    {
                        return BadRequest(new { error = "Only JPEG and PNG images are allowed." });
                    }

                    if (updateUserDto.ProfilePicture.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest(new { error = "File size should not exceed 5MB." });
                    }

                    // Read the image into a byte array
                    using var memoryStream = new MemoryStream();
                    await updateUserDto.ProfilePicture.CopyToAsync(memoryStream);
                    using var originalImage = Image.FromStream(memoryStream);

                    // Calculate the new dimensions while maintaining the aspect ratio
                    int newWidth = 200;
                    int newHeight = (int)(originalImage.Height * (200.0 / originalImage.Width));

                    // Create a new bitmap with the new dimensions
                    using var resizedImage = new Bitmap(newWidth, newHeight);
                    using (var graphics = Graphics.FromImage(resizedImage))
                    {
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
                    }

                    // Save the resized image to a byte array
                    using var outputMemoryStream = new MemoryStream();
                    resizedImage.Save(outputMemoryStream, ImageFormat.Jpeg);
                    user.ProfilePicture = outputMemoryStream.ToArray();
                }

                var updatedUser = await _userService.UpdateUser(user);

                return Ok(new { message = "User updated successfully.", user = updatedUser });
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred while updating the user: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }


        [HttpGet("ProfilePicture/{userId}")]
        public async Task<IActionResult> GetProfilePicture(int userId)
        {
            try
            {
                var user = await _userService.GetUser(userId);

                if (user == null || user.ProfilePicture == null)
                {
                    return NotFound();
                }

                return File(user.ProfilePicture, "image/jpeg");
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred while retrieving the profile picture: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

    }
}
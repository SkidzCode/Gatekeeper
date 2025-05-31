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
using GateKeeper.Server.Models.Account.UserModels;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using GateKeeper.Server.Extension;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// API controller for handling user-related operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IDbHelper _dbHelper;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly ILogger<UserController> _logger;
        
        /// <summary>
        /// Constructor for the UserController.
        /// </summary>
        /// <param name="configuration">Application configuration dependency.</param>
        /// <param name="jwtService">JWT service dependency.</param>
        public UserController(
            // IConfiguration configuration, // Removed
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
        /// Updates user information.
        /// </summary>
        /// <param name="user">User object containing updated details.</param>
        /// <returns>Action result indicating success or failure.</returns>
        [HttpPost("Update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int userId = GetUserIdFromClaims();
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();

            try
            {
                await _userService.UpdateUser(user);
                await _userService.UpdateUserRoles(user.Id, user.Roles);
                return Ok(new { message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
            finally
            {
                _logger.LogInformation("User update attempted: {UserId}, IP: {IpAddress}, Device: {UserAgent}", userId, userIp, userAgent);
            }
        }

        /// <summary>
        /// Retrieves profile information of the authenticated user.
        /// </summary>
        /// <returns>User profile details if successful or error if not.</returns>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            int userId = GetUserIdFromClaims();
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            try
            {
                User? user = await _userService.GetUser(userId);

                if (user != null) return Ok(user);
                _logger.LogWarning("User not found: {UserId}, IP: {IpAddress}", userId, userIp);
                return NotFound("User not found.");

            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
            finally
            {
                _logger.LogInformation("User profile retrieval attempted: {UserId}, IP: {IpAddress}", userId, userIp);
            }
        }

        /// <summary>
        /// Retrieves a list of all users.
        /// </summary>
        /// <returns>List of users.</returns>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers()
        {
            int adminUserId = GetUserIdFromClaims();
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            try
            {
                List<User?> users = await _userService.GetUsers();
                return Ok(users);
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
            finally
            {
                _logger.LogInformation("User list retrieval attempted by Admin: {AdminUserId}, IP: {IpAddress}", adminUserId, userIp);
            }
        }

        /// <summary>
        /// Retrieves user information by user ID.
        /// </summary>
        /// <param name="userId">ID of the user to retrieve.</param>
        /// <returns>User details.</returns>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers(int userId)
        {
            int adminUserId = GetUserIdFromClaims();
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            try
            {
                User? user = await _userService.GetUser(userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found: RequestedUserId: {UserId}, AdminUserId: {AdminUserId}, IP: {IpAddress}", userId, adminUserId, userIp);
                    return NotFound("User not found.");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
            finally
            {
                _logger.LogInformation("User retrieval attempted: RequestedUserId: {UserId}, AdminUserId: {AdminUserId}, IP: {IpAddress}", userId, adminUserId, userIp);
            }
        }

        /// <summary>
        /// Retrieves user information and roles for editing by admin.
        /// </summary>
        /// <param name="userId">ID of the user to edit.</param>
        /// <returns>User and roles information.</returns>
        [HttpGet("user/edit/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsersEdit(int userId)
        {
            int adminUserId = GetUserIdFromClaims();
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            try
            {
                User? user = await _userService.GetUser(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for edit: RequestedUserId: {UserId}, AdminUserId: {AdminUserId}, IP: {IpAddress}", userId, adminUserId, userIp);
                    return NotFound("User not found.");
                }

                List<Role> roles = await _roleService.GetAllRoles();
                return Ok(new { user, roles });
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
            finally
            {
                _logger.LogInformation("User edit retrieval attempted: RequestedUserId: {UserId}, AdminUserId: {AdminUserId}, IP: {IpAddress}", userId, adminUserId, userIp);
            }
        }

        /// <summary>
        /// Updates user information, including profile picture.
        /// </summary>
        /// <param name="updateUserDto">User update data transfer object.</param>
        /// <returns>Action result indicating success or failure.</returns>
        [HttpPost("UpdateWithImage")]
        [Authorize]
        public async Task<IActionResult> UpdateUserWithImage([FromForm] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int userId = GetUserIdFromClaims();
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            string userAgent = Request.Headers["User-Agent"].ToString().SanitizeForLogging();

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
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
            finally
            {
                _logger.LogInformation("User update with image attempted: {UserId}, IP: {IpAddress}, Device: {UserAgent}", userId, userIp, userAgent);
            }
        }

        /// <summary>
        /// Retrieves the profile picture of a user.
        /// </summary>
        /// <param name="userId">ID of the user.</param>
        /// <returns>Profile picture as an image file.</returns>
        [HttpGet("ProfilePicture/{userId}")]
        public async Task<IActionResult> GetProfilePicture(int userId)
        {
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            try
            {
                var user = await _userService.GetUser(userId);

                if (user == null || user.ProfilePicture == null)
                {
                    // _logger.LogWarning("Profile picture not found: RequestedUserId: {UserId}, IP: {IpAddress}", userId, userIp);
                    return NotFound();
                }

                return File(user.ProfilePicture, "image/jpeg");
            }
            catch (Exception ex)
            {
                // Removed generic catch block, error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
            finally
            {
                _logger.LogInformation("Profile picture retrieval attempted: RequestedUserId: {UserId}, IP: {IpAddress}", userId, userIp);
            }
        }

        #region Private Functions

        private int GetUserIdFromClaims()
        {
            return int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId) ? userId : 0;
        }

        #endregion
    }
}
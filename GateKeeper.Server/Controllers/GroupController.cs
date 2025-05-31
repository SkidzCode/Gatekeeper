using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// API controller for handling user-related operations such as registration, login, token refresh, and profile retrieval.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly IDbHelper _dbHelper;
        // Service for handling JSON Web Token (JWT) operations
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// Constructor for the UserController.
        /// </summary>
        // <param name="configuration">Application configuration dependency.</param> // Removed IConfiguration
        /// <param name="jwtService">JWT service dependency.</param>
        public GroupController(IDbHelper dbHelper, IUserService userService, ILogger<UserController> logger) // Removed IConfiguration
        {
            _userService = userService;
            _dbHelper = dbHelper;
            _logger = logger;
        }

        /// <summary>
        /// API endpoint to retrieve list of users.
        /// </summary>
        /// <returns>List of users</returns>
        [HttpGet("Groups")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGroups()
        {
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
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using GateKeeper.Server.Resources; // If you have resource files for localized error messages
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace GateKeeper.Server.Controllers
{
    /// <summary>
    /// API controller for handling role-related operations such as retrieval, insertion, and updating.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RoleController> _logger;

        /// <summary>
        /// Constructor for the RoleController.
        /// </summary>
        /// <param name="roleService">Role service dependency.</param>
        /// <param name="logger">Logger dependency.</param>
        public RoleController(IRoleService roleService, ILogger<RoleController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all roles from the system.
        /// </summary>
        /// <returns>A list of roles.</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _roleService.GetAllRoles();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                // If you have resource strings, you can use them here. Otherwise, a fixed message is fine.
                var errorMessage = $"Error occurred while fetching all roles: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Retrieves a single role by Id.
        /// </summary>
        /// <param name="id">The unique identifier of the role.</param>
        /// <returns>The requested role or 404 if not found.</returns>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetRoleById(int id)
        {
            try
            {
                var role = await _roleService.GetRoleById(id);
                if (role == null)
                {
                    return NotFound(new { message = $"Role with Id {id} not found." });
                }
                return Ok(role);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error retrieving role with Id {id}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Retrieves a single role by name.
        /// </summary>
        /// <param name="roleName">The name of the role.</param>
        /// <returns>The requested role or 404 if not found.</returns>
        [HttpGet("by-name/{roleName}")]
        [Authorize]
        public async Task<IActionResult> GetRoleByName(string roleName)
        {
            try
            {
                var role = await _roleService.GetRoleByName(roleName);
                if (role == null)
                {
                    return NotFound(new { message = $"Role '{roleName}' not found." });
                }
                return Ok(role);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error retrieving role with name '{roleName}': {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Creates a new role.
        /// </summary>
        /// <param name="role">A Role object containing the name of the new role.</param>
        /// <returns>The newly created role.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddRole([FromBody] Role role)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdRole = await _roleService.AddRole(role);
                return Ok(new { message = "Role created successfully.", role = createdRole });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error creating new role: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        /// <param name="id">The Id of the role to update.</param>
        /// <param name="role">The new data for the role (role name, etc.).</param>
        /// <returns>The updated role.</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] Role role)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Ensure the role object's Id matches the route
                role.Id = id;

                var updatedRole = await _roleService.UpdateRole(role);
                return Ok(new { message = "Role updated successfully.", role = updatedRole });
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error updating role with Id {id}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}

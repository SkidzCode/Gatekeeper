using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Services.Site
{
    public class RoleService(IRoleRepository roleRepository, ILogger<RoleService> logger) : IRoleService
    {
        private readonly IRoleRepository _roleRepository = roleRepository;
        private readonly ILogger<RoleService> _logger = logger;

        /// <summary>
        /// Inserts a new Role.
        /// </summary>
        /// <param name="role">Role object containing RoleName.</param>
        /// <returns>The inserted Role (with any DB-generated fields, if applicable).</returns>
        public async Task<Role> AddRole(Role role)
        {
            // Business logic can be added here if needed before/after calling the repository
            _logger.LogInformation("Adding role: {RoleName}", role.RoleName);
            var addedRole = await _roleRepository.AddRoleAsync(role);
            _logger.LogInformation("Added role with ID: {RoleId}", addedRole.Id);
            return addedRole;
        }

        /// <summary>
        /// Gets a single Role by id.
        /// </summary>
        /// <param name="id">Unique Id of the Role.</param>
        /// <returns>Role object or null if not found.</returns>
        public async Task<Role?> GetRoleById(int id)
        {
            _logger.LogInformation("Getting role by ID: {RoleId}", id);
            return await _roleRepository.GetRoleByIdAsync(id);
        }

        /// <summary>
        /// Gets a single Role by name.
        /// </summary>
        /// <param name="roleName">Name of the role.</param>
        /// <returns>Role object or null if not found.</returns>
        public async Task<Role?> GetRoleByName(string roleName)
        {
            _logger.LogInformation("Getting role by Name: {RoleName}", roleName);
            return await _roleRepository.GetRoleByNameAsync(roleName);
        }

        /// <summary>
        /// Updates a Role.
        /// </summary>
        /// <param name="role">Role object containing id and (optionally) a new RoleName.</param>
        /// <returns>The updated Role.</returns>
        public async Task<Role> UpdateRole(Role role)
        {
            _logger.LogInformation("Updating role with ID: {RoleId}", role.Id);
            return await _roleRepository.UpdateRoleAsync(role);
        }

        /// <summary>
        /// Retrieves all Roles.
        /// </summary>
        /// <returns>List of Role objects.</returns>
        public async Task<List<Role>> GetAllRoles()
        {
            _logger.LogInformation("Getting all roles");
            return await _roleRepository.GetAllRolesAsync();
        }
    }
}
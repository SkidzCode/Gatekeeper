using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Models;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for authorization and access control operations.
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="roleName">The name of the role to assign.</param>
        /// <returns>Whether the operation was successful.</returns>
        Task<bool> AssignRoleAsync(int userId, string roleName);

        /// <summary>
        /// Removes a role from a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="roleName">The name of the role to remove.</param>
        /// <returns>Whether the operation was successful.</returns>
        Task<bool> RemoveRoleAsync(int userId, string roleName);

        /// <summary>
        /// Checks if a user has a specific role.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="roleName">The name of the role to check.</param>
        /// <returns>Whether the user has the role.</returns>
        Task<bool> UserHasRoleAsync(int userId, string roleName);

        /// <summary>
        /// Defines and assigns permissions based on user attributes.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="attributes">The attribute-based conditions to define permissions.</param>
        /// <returns>Whether the operation was successful.</returns>
        Task<bool> AssignPermissionsBasedOnAttributesAsync(int userId, Dictionary<string, string> attributes);

        /// <summary>
        /// Validates if a user is authorized to perform a specific action.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="action">The action to validate.</param>
        /// <param name="resource">The resource the action is being performed on.</param>
        /// <returns>Whether the user is authorized.</returns>
        Task<bool> IsUserAuthorizedAsync(int userId, string action, string resource);

        /// <summary>
        /// Retrieves all roles assigned to a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of roles assigned to the user.</returns>
        Task<IEnumerable<string>> GetUserRolesAsync(int userId);

        /// <summary>
        /// Middleware to enforce route-level protection based on permissions.
        /// </summary>
        /// <param name="route">The route to protect.</param>
        /// <param name="userId">The ID of the user attempting to access the route.</param>
        /// <returns>Whether access to the route is allowed.</returns>
        Task<bool> EnforceRouteProtectionAsync(string route, int userId);

        /// <summary>
        /// Retrieves all permissions granted to a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of permissions assigned to the user.</returns>
        Task<IEnumerable<string>> GetUserPermissionsAsync(int userId);

        /// <summary>
        /// Revokes all permissions from a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>Whether the operation was successful.</returns>
        Task<bool> RevokeAllPermissionsAsync(int userId);

        /// <summary>
        /// Lists all available roles in the system.
        /// </summary>
        /// <returns>A list of available roles.</returns>
        Task<IEnumerable<string>> GetAllRolesAsync();

        /// <summary>
        /// Lists all available permissions in the system.
        /// </summary>
        /// <returns>A list of available permissions.</returns>
        Task<IEnumerable<string>> GetAllPermissionsAsync();
    }
}

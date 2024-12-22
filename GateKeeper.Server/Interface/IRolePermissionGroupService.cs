using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Models;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for role, permission, and group management operations.
    /// </summary>
    public interface IRolePermissionGroupService
    {
        #region Role Management

        /// <summary>
        /// Creates a new role.
        /// </summary>
        /// <param name="roleName">The name of the role to create.</param>
        /// <returns>The ID of the created role.</returns>
        Task<int> CreateRoleAsync(string roleName);

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        /// <param name="roleId">The ID of the role to update.</param>
        /// <param name="newRoleName">The new name for the role.</param>
        /// <returns>Whether the update was successful.</returns>
        Task<bool> UpdateRoleAsync(int roleId, string newRoleName);

        /// <summary>
        /// Deletes a role.
        /// </summary>
        /// <param name="roleId">The ID of the role to delete.</param>
        /// <returns>Whether the deletion was successful.</returns>
        Task<bool> DeleteRoleAsync(int roleId);

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="roleId">The ID of the role to assign.</param>
        /// <returns>Whether the assignment was successful.</returns>
        Task<bool> AssignRoleToUserAsync(int userId, int roleId);

        /// <summary>
        /// Removes a role from a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="roleId">The ID of the role to remove.</param>
        /// <returns>Whether the removal was successful.</returns>
        Task<bool> RemoveRoleFromUserAsync(int userId, int roleId);

        #endregion

        #region Permission Management

        /// <summary>
        /// Creates a new permission.
        /// </summary>
        /// <param name="permissionName">The name of the permission to create.</param>
        /// <returns>The ID of the created permission.</returns>
        Task<int> CreatePermissionAsync(string permissionName);

        /// <summary>
        /// Associates a permission with a role.
        /// </summary>
        /// <param name="roleId">The ID of the role.</param>
        /// <param name="permissionId">The ID of the permission to associate.</param>
        /// <returns>Whether the association was successful.</returns>
        Task<bool> AssignPermissionToRoleAsync(int roleId, int permissionId);

        /// <summary>
        /// Removes a permission from a role.
        /// </summary>
        /// <param name="roleId">The ID of the role.</param>
        /// <param name="permissionId">The ID of the permission to remove.</param>
        /// <returns>Whether the removal was successful.</returns>
        Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId);

        /// <summary>
        /// Assigns a custom permission to a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="permissionId">The ID of the permission to assign.</param>
        /// <returns>Whether the assignment was successful.</returns>
        Task<bool> AssignCustomPermissionToUserAsync(int userId, int permissionId);

        /// <summary>
        /// Removes a custom permission from a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="permissionId">The ID of the permission to remove.</param>
        /// <returns>Whether the removal was successful.</returns>
        Task<bool> RemoveCustomPermissionFromUserAsync(int userId, int permissionId);

        #endregion

        #region Group Management

        /// <summary>
        /// Creates a new user group.
        /// </summary>
        /// <param name="groupName">The name of the group to create.</param>
        /// <returns>The ID of the created group.</returns>
        Task<int> CreateGroupAsync(string groupName);

        /// <summary>
        /// Updates an existing group.
        /// </summary>
        /// <param name="groupId">The ID of the group to update.</param>
        /// <param name="newGroupName">The new name for the group.</param>
        /// <returns>Whether the update was successful.</returns>
        Task<bool> UpdateGroupAsync(int groupId, string newGroupName);

        /// <summary>
        /// Deletes a user group.
        /// </summary>
        /// <param name="groupId">The ID of the group to delete.</param>
        /// <returns>Whether the deletion was successful.</returns>
        Task<bool> DeleteGroupAsync(int groupId);

        /// <summary>
        /// Adds a user to a group.
        /// </summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="userId">The ID of the user to add.</param>
        /// <returns>Whether the addition was successful.</returns>
        Task<bool> AddUserToGroupAsync(int groupId, int userId);

        /// <summary>
        /// Removes a user from a group.
        /// </summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="userId">The ID of the user to remove.</param>
        /// <returns>Whether the removal was successful.</returns>
        Task<bool> RemoveUserFromGroupAsync(int groupId, int userId);

        /// <summary>
        /// Assigns roles or permissions to a group.
        /// </summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="roleIds">List of role IDs to assign.</param>
        /// <param name="permissionIds">List of permission IDs to assign.</param>
        /// <returns>Whether the assignments were successful.</returns>
        Task<bool> AssignRolesAndPermissionsToGroupAsync(int groupId, IEnumerable<int> roleIds, IEnumerable<int> permissionIds);

        #endregion
    }
}

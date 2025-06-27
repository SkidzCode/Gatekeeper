using GateKeeper.Server.Models.Account.UserModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for the UserRepository, defining methods for user-related database operations using Dapper.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves a user by their unique ID.
        /// Includes user roles.
        /// </summary>
        /// <param name="id">The user's ID.</param>
        /// <returns>A User object if found, otherwise null.</returns>
        Task<User?> GetUserByIdAsync(int id);

        /// <summary>
        /// Retrieves a user by their username or email.
        /// Includes user roles.
        /// </summary>
        /// <param name="identifier">The user's username or email.</param>
        /// <returns>A User object if found, otherwise null.</returns>
        Task<User?> GetUserByIdentifierAsync(string identifier);

        /// <summary>
        /// Retrieves all users.
        /// </summary>
        /// <returns>A list of User objects.</returns>
        Task<IEnumerable<User>> GetAllUsersAsync();

        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="user">The user object to add (Password should be pre-hashed).</param>
        /// <param name="salt">The salt used for password hashing.</param>
        /// <returns>A tuple containing the new user's ID (if successful) and a result code (0 for success, 1 for email exists, 2 for username exists, 3 for both).</returns>
        Task<(int? userId, int resultCode)> AddUserAsync(User user, string salt);

        /// <summary>
        /// Updates an existing user's profile information (excluding password and roles).
        /// </summary>
        /// <param name="user">The user object with updated information.</param>
        /// <returns>True if the update was successful, otherwise false.</returns>
        Task<bool> UpdateUserAsync(User user);

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="hashedPassword">The new, pre-hashed password.</param>
        /// <param name="salt">The salt used for the new password.</param>
        /// <returns>True if the password change was successful, otherwise false.</returns>
        Task<bool> ChangePasswordAsync(int userId, string hashedPassword, string salt);

        /// <summary>
        /// Retrieves all role names for a given user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of role names.</returns>
        Task<IEnumerable<string>> GetUserRolesAsync(int userId);

        /// <summary>
        /// Updates the roles assigned to a user.
        /// This typically involves clearing existing roles and adding the new ones.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="roleNames">A list of role names to assign to the user.</param>
        Task UpdateUserRolesAsync(int userId, IEnumerable<string> roleNames);

        /// <summary>
        /// Checks if a username already exists.
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <returns>True if the username exists, otherwise false.</returns>
        Task<bool> UsernameExistsAsync(string username);

        /// <summary>
        /// Checks if an email address already exists.
        /// </summary>
        /// <param name="email">The email to check.</param>
        /// <returns>True if the email exists, otherwise false.</returns>
        Task<bool> EmailExistsAsync(string email);
    }
}

using Microsoft.AspNetCore.Mvc;
using GateKeeper.Server.Models.Account.UserModels;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for the UserService class, defining methods for user-related operations.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Adds a new user to the system.
        /// </summary>
        /// <param name="user">User object containing registration details.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task<RegistrationResponse> RegisterUser(User user);

        Task<User?> GetUser(string identifier);
        Task<User?> GetUser(int identifier);

        Task<List<string>> GetRolesAsync(int Id);

        Task<int> ChangePassword(int userId, string newPassword);

        Task<User> UpdateUser(User user);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<List<User>> GetUsers();
    }
}
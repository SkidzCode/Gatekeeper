using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account.UserModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System; // Required for DBNull

namespace GateKeeper.Server.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        /// <summary>
        /// Constructor for the UserService.
        /// </summary>
        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="user">User object containing registration details.</param>
        /// <returns>RegistrationResponse indicating success or failure.</returns>
        public async Task<RegistrationResponse> RegisterUser(User user)
        {
            var response = new RegistrationResponse
            {
                User = user // Keep original user details for the response
            };

            // Step 1: Generate a unique salt and hash the user's password
            var salt = PasswordHelper.GenerateSalt();
            // Create a temporary user object for password hashing to avoid modifying the input 'user' object's password directly yet
            var userToRegister = new User
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Username = user.Username,
                Password = PasswordHelper.HashPassword(user.Password, salt), // Hash here
                Phone = user.Phone
                // Id will be set by the repository if successful
            };

            _logger.LogInformation("Attempting to register user: {Username}", userToRegister.Username);

            // Step 2: Call repository to add user
            var (newUserId, resultCode) = await _userRepository.AddUserAsync(userToRegister, salt);

            response.FailureReason = resultCode switch
            {
                1 => "Email already exists.",
                2 => "Username already exists.",
                3 => "Both Email and Username already exist.",
                _ => null // Success or other DB issue (which should throw exception ideally)
            };

            if (newUserId.HasValue && newUserId.Value > 0 && resultCode == 0)
            {
                response.IsSuccessful = true;
                response.User.Id = newUserId.Value;
                // The password in response.User should remain the original plain text for confirmation if needed,
                // or be cleared. For security, let's clear it.
                response.User.Password = string.Empty;
                _logger.LogInformation("User {Username} registered successfully with ID: {UserId}", userToRegister.Username, newUserId.Value);
            }
            else
            {
                response.IsSuccessful = false;
                _logger.LogWarning("User registration failed for {Username}. Reason: {FailureReason}", userToRegister.Username, response.FailureReason);
            }
            return response;
        }

        public async Task<int> ChangePassword(int userId, string newPassword)
        {
            _logger.LogInformation("Attempting to change password for user ID: {UserId}", userId);
            var salt = PasswordHelper.GenerateSalt();
            var hashedPassword = PasswordHelper.HashPassword(newPassword, salt);

            var success = await _userRepository.ChangePasswordAsync(userId, hashedPassword, salt);
            if (success)
            {
                _logger.LogInformation("Password changed successfully for user ID: {UserId}", userId);
                return 1; // Consistent with old return type, though bool might be better
            }
            _logger.LogWarning("Failed to change password for user ID: {UserId}", userId);
            return 0; // Or throw an exception
        }

        public async Task<List<string>> GetRolesAsync(int id)
        {
            _logger.LogDebug("Fetching roles for user ID: {UserId}", id);
            var roles = await _userRepository.GetUserRolesAsync(id);
            return roles.ToList();
        }

        public async Task<User?> GetUser(string identifier)
        {
            _logger.LogDebug("Fetching user by identifier: {Identifier}", identifier);
            // The repository method GetUserByIdentifierAsync already includes roles
            return await _userRepository.GetUserByIdentifierAsync(identifier);
        }

        public async Task<User?> GetUser(int id)
        {
            _logger.LogDebug("Fetching user by ID: {UserId}", id);
            // The repository method GetUserByIdAsync already includes roles
            return await _userRepository.GetUserByIdAsync(id);
        }

        public async Task<User> UpdateUser(User user)
        {
            _logger.LogInformation("Attempting to update user ID: {UserId}", user.Id);
            // Password and Salt are not updated here. Roles are updated via UpdateUserRoles.
            // The repository's UpdateUserAsync is expected to handle this.
            var success = await _userRepository.UpdateUserAsync(user);
            if (success)
            {
                _logger.LogInformation("User ID: {UserId} updated successfully.", user.Id);
                // Optionally, re-fetch the user to get any DB-generated changes (e.g., UpdatedAt)
                // For now, returning the passed user object as per the old method's behavior.
                return user;
            }
            _logger.LogWarning("Failed to update user ID: {UserId}", user.Id);
            // Consider throwing an exception if the update fails, or changing return type to bool/User?
            // For consistency with the old signature, returning the user object, but it might not reflect the actual DB state if update failed.
            // This behavior should be documented or changed.
            // For now, let's return the user as is, but if an error occurred an exception should have been thrown by repo or dapper.
            // If rowsAffected was 0, it means user was not found or data was identical.
            return user;
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            _logger.LogDebug("Checking if username exists: {Username}", username);
            return await _userRepository.UsernameExistsAsync(username);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            _logger.LogDebug("Checking if email exists: {Email}", email);
            return await _userRepository.EmailExistsAsync(email);
        }

        public async Task<List<User>> GetUsers()
        {
            _logger.LogInformation("Fetching all users.");
            var users = await _userRepository.GetAllUsersAsync();
            // Note: GetAllUsersAsync from repository currently does not fetch roles for each user to avoid N+1.
            // This is consistent with the previous implementation of GetUsers.
            // If roles are needed here, the IUserRepository.GetAllUsersAsync and its implementation would need adjustment.
            return users.ToList();
        }

        public async Task UpdateUserRoles(int userId, List<string> roleNames)
        {
            _logger.LogInformation("Updating roles for user ID: {UserId}", userId);
            await _userRepository.UpdateUserRolesAsync(userId, roleNames);
            _logger.LogInformation("Roles updated successfully for user ID: {UserId}", userId);
        }
    }
}

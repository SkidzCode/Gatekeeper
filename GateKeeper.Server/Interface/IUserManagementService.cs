using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Models.Account;

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for user management-related operations.
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// Retrieves a user's profile details by their ID.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The user's profile details.</returns>
        Task<UserProfile> GetUserProfileAsync(int userId);

        /// <summary>
        /// Updates a user's profile details.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="profileUpdate">The updated profile details.</param>
        /// <returns>Whether the update was successful.</returns>
        Task<bool> UpdateUserProfileAsync(int userId, UserProfileUpdateRequest profileUpdate);

        /// <summary>
        /// Uploads or updates a user's profile picture.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="profilePicture">The profile picture file data.</param>
        /// <returns>The URL of the uploaded profile picture.</returns>
        Task<string> UploadProfilePictureAsync(int userId, byte[] profilePicture);

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="userCreationRequest">The details for the new user.</param>
        /// <returns>The ID of the newly created user.</returns>
        Task<int> CreateUserAsync(UserCreationRequest userCreationRequest);

        /// <summary>
        /// Retrieves a list of users matching specified criteria.
        /// </summary>
        /// <param name="filter">Optional filters for retrieving users.</param>
        /// <returns>A list of users.</returns>
        Task<IEnumerable<User>> GetUsersAsync(UserFilterCriteria filter);

        /// <summary>
        /// Updates user details.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="userUpdateRequest">The details to update.</param>
        /// <returns>Whether the update was successful.</returns>
        Task<bool> UpdateUserAsync(int userId, UserUpdateRequest userUpdateRequest);

        /// <summary>
        /// Deletes a user by their ID.
        /// </summary>
        /// <param name="userId">The ID of the user to delete.</param>
        /// <returns>Whether the deletion was successful.</returns>
        Task<bool> DeleteUserAsync(int userId);

        /// <summary>
        /// Imports a bulk list of users from a CSV file.
        /// </summary>
        /// <param name="csvData">The CSV file data.</param>
        /// <returns>A report on the import results.</returns>
        Task<BulkImportResult> ImportUsersAsync(byte[] csvData);

        /// <summary>
        /// Exports a list of users to a CSV file.
        /// </summary>
        /// <param name="filter">Optional filters for exporting users.</param>
        /// <returns>The CSV file data.</returns>
        Task<byte[]> ExportUsersAsync(UserFilterCriteria filter);

        /// <summary>
        /// Activates or deactivates a user's account.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="isActive">The desired active status.</param>
        /// <returns>Whether the status update was successful.</returns>
        Task<bool> SetUserActiveStatusAsync(int userId, bool isActive);

        /// <summary>
        /// Updates a user's email verification status.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="isVerified">The verification status.</param>
        /// <returns>Whether the status update was successful.</returns>
        Task<bool> SetEmailVerificationStatusAsync(int userId, bool isVerified);

        /// <summary>
        /// Retrieves user activity logs.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of activity logs.</returns>
        Task<IEnumerable<UserActivityLog>> GetUserActivityLogsAsync(int userId);

        /// <summary>
        /// Retrieves logs of account creation and modifications.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of account creation and modification logs.</returns>
        Task<IEnumerable<AccountLog>> GetAccountLogsAsync(int userId);
    }
}

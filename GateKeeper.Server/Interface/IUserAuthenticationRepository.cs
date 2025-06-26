using System.Threading.Tasks;
using GateKeeper.Server.Models.Account; // For User model if needed in methods
using GateKeeper.Server.Models.Account.Login; // For TokenVerificationResponse if needed
using MySqlConnector; // If any MySql specific types are exposed, though ideally not

namespace GateKeeper.Server.Interface
{
    /// <summary>
    /// Interface for repository handling database operations related to user authentication.
    /// </summary>
    public interface IUserAuthenticationRepository
    {
        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="roleName">The name of the role to assign.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AssignRoleToUserAsync(int userId, string roleName);

        /// <summary>
        /// Finalizes the validation process for a user, typically after email verification.
        /// </summary>
        /// <param name="userId">The ID of the user being validated.</param>
        /// <param name="sessionId">The session ID associated with the validation attempt (e.g., from the verification token).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ValidateFinishAsync(int userId, string sessionId);

        // Add other methods that were previously using _dbHelper in UserAuthenticationService
        // For example, if there were direct DB calls for lockout mechanisms, token storage (beyond IVerifyTokenService), etc.
        // Based on the provided UserAuthenticationService.cs, the primary direct DB calls were:
        // 1. AssignRoleToUser (covered by AssignRoleToUserAsync)
        // 2. ValidateFinish (covered by ValidateFinishAsync)

        // If UserAuthenticationService directly handled other DB operations that are not abstracted
        // by other services (IUserService, IVerificationService, ISessionService, etc.),
        // those would be added here.
        // For now, these two seem to be the main candidates for this new repository from UserAuthenticationService's direct DB calls.
    }
}

using GateKeeper.Server.Models.Account.UserModels; // For User model if we embed it

namespace GateKeeper.Server.Models.Account
{
    /// <summary>
    /// Holds the details of a verification token retrieved from the database.
    /// </summary>
    public class VerificationTokenDetails
    {
        public bool Revoked { get; set; }
        public bool Complete { get; set; }
        public string VerifyType { get; set; } = string.Empty;
        public string RefreshSalt { get; set; } = string.Empty; // This is the token's salt
        public string HashedToken { get; set; } = string.Empty;
        public int UserId { get; set; }

        // User details - these are returned by the current "ValidateUser" SP
        // Consider if these should be populated by a separate call to IUserService
        // or if the SP should continue to return them. For now, matching the SP.
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string UserSalt { get; set; } = string.Empty; // User's main password salt
        public string UserPassword { get; set; } = string.Empty; // User's main password hash
        public string Username { get; set; } = string.Empty;
        // Roles will likely still be fetched via IUserService.GetRolesAsync in the service layer.
    }
}

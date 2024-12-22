using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Account
{

    /// <summary>
    /// Request model for changing password.
    /// </summary>
    public class PasswordChangeRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(255, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
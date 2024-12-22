using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Account;
/// <summary>
/// Request model for password reset.
/// </summary>
public class PasswordResetRequest
{
    [Required]
    public string ResetToken { get; set; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}
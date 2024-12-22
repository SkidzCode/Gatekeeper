using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Account;

/// <summary>
/// Request model for validating password strength.
/// </summary>
public class ValidatePasswordRequest
{
    [Required]
    [StringLength(255, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}
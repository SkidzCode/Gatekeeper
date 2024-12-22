using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Account;

/// <summary>
/// Request model for user login.
/// </summary>
public class UserLoginRequest
{
    [Required]
    public string Identifier { get; set; } = string.Empty; // Can be email or username

    [Required]
    public string Password { get; set; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Account;

/// <summary>
/// Request model for refresh token.
/// </summary>
public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
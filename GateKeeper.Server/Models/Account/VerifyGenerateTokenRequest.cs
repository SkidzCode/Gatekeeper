using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Account;

public class VerifyGenerateTokenRequest
{
    /// <summary>
    /// The verification code to validate.
    /// </summary>
    [Required]
    public int UserId { get; set; }
    [Required]
    public string TokenType { get; set; }
}
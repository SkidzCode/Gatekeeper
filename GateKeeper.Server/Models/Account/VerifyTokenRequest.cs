using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Account;


/// <summary>
/// Request model for verifying user.
/// </summary>
public class VerifyTokenRequest
{
    [Required]
    public string VerificationCode { get; set; }
    [Required]
    public string TokenType { get; set; }

}
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Account;

public class VerifyUserRequest
{
    [Required]
    public string VerificationCode { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Account.Login;

public class TokenVerificationResponse
{
    public bool IsVerified { get; set; }
    public User? User { get; set; }
    public string VerificationCode { get; set; }
    public string SessionId { get; set; }
    public string TokenType { get; set; }
    public string? FailureReason { get; set; }
}
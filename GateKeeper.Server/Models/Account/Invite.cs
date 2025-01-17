namespace GateKeeper.Server.Models.Account;

public class Invite
{
    public int Id { get; set; }
    public int FromId { get; set; }
    public string? ToName { get; set; }
    public string? ToEmail { get; set; }
    public string? VerificationId { get; set; }
    public int? NotificationId { get; set; }
    public DateTime Created { get; set; }

    // Fields from JOIN:
    public bool IsExpired { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsComplete { get; set; }
    public bool IsSent { get; set; }

    // Misc
    public string Website { get; set; }
}
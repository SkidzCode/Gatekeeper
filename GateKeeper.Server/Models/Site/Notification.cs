namespace GateKeeper.Server.Models.Site;

public class Notification
{
    public int Id { get; set; }
    public int RecipientId { get; set; }
    public int FromId { get; set; }
    public string ToName { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string Channel { get; set; } = "email"; // or "sms", "push", "inapp"
    public string URL { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsSent { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
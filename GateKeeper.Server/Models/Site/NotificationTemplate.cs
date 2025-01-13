namespace GateKeeper.Server.Models.Site;

public class NotificationTemplate
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = default!;
    public string Channel { get; set; } = default!;
    public string? TokenType { get; set; } = null;
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
namespace GateKeeper.Server.Models.Account;

public class EmailRequest
{
    public string ToName { get; set; }
    public string ToEmail { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}
namespace GateKeeper.Server.Models.Account;

/// <summary>
/// Request model for logout.
/// </summary>
public class LogoutRequest
{
    public string? Token { get; set; }
}
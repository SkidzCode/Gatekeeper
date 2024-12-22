namespace GateKeeper.Server.Models.Account;

/// <summary>
/// Request model for logging out from a device.
/// </summary>
public class LogoutDeviceRequest
{
    public string? SessionId { get; set; }
}
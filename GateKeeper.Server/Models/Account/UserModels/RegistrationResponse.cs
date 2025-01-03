namespace GateKeeper.Server.Models.Account.UserModels;

public class RegistrationResponse
{
    public bool IsSuccessful { get; set; } = false;
    public string FailureReason { get; set; }
    public User User { get; set; }
}
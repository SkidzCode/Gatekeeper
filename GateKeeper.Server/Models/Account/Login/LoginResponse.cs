using GateKeeper.Server.Models.Account.UserModels;
using GateKeeper.Server.Models.Site;

namespace GateKeeper.Server.Models.Account.Login;

public class LoginResponse
{
    public bool IsSuccessful { get; set; }
    public string? SessionId { get; set; } = Guid.NewGuid().ToString();
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public User? User { get; set; }
    public List<Setting> Settings { get; set; } = new List<Setting>();

    /// <summary>
    /// This is not returned to the user and is used for logging only
    /// </summary>
    public string? FailureReason { get; set; }
    public int Attempts { get; set; } = 0;
    public bool ToMany { get; set; }
}
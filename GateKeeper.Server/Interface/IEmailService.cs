using GateKeeper.Server.Models.Account.UserModels;

namespace GateKeeper.Server.Interface;

public interface IEmailService
{
    public Task SendEmailAsync(string email, string subject, string message);
    public Task SendEmailAsync(User user, string url, string verificationCode, string subject, string message);
}
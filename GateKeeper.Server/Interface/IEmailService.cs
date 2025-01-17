using GateKeeper.Server.Models.Account.UserModels;

namespace GateKeeper.Server.Interface;

public interface IEmailService
{
    public Task SendEmailAsync(string email, string subject, string message);
    public Task SendEmailAsync(string toEmail, string toName, string fromName2, string subject, string message);
}
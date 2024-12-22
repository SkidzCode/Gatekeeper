namespace GateKeeper.Server.Interface;

public interface IEmailService
{
    public Task SendEmailAsync(string email, string subject, string message);

}
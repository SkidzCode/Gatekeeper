using System.Net;
using System.Net.Mail;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account.UserModels;
using Microsoft.Extensions.Configuration;

namespace GateKeeper.Server.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            // Get all SMTP/email settings from user secrets
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            var userName = _configuration["EmailSettings:UserName"];
            var password = _configuration["EmailSettings:Password"];
            var fromName = _configuration["EmailSettings:FromName"];
            var useSsl = bool.Parse(_configuration["EmailSettings:UseSsl"] ?? "true");

            // Configure the email client
            using var smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(userName, password),
                EnableSsl = useSsl
            };

            // Create the email message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(userName, fromName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true, // Change to true if sending HTML content
            };

            mailMessage.To.Add(new MailAddress(email, email));

            // Send the email
            smtpClient.Send(mailMessage);
        }

        public async Task SendEmailAsync(string toEmail, string toName, string fromName2, string subject, string message)
        {
            // Get all SMTP/email settings from user secrets
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            var userName = _configuration["EmailSettings:UserName"];
            var password = _configuration["EmailSettings:Password"];
            var useSsl = bool.Parse(_configuration["EmailSettings:UseSsl"] ?? "true");
            
            // Configure the email client
            using var smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(userName, password),
                EnableSsl = useSsl
            };

            // Create the email message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(userName, fromName2),
                Subject = subject,
                Body = message,
                IsBodyHtml = true, // Change to true if sending HTML content
            };

            mailMessage.To.Add(new MailAddress(toEmail, toName));

            // Send the email
            smtpClient.Send(mailMessage);
        }
    }
}
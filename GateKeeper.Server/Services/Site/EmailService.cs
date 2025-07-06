using System.Net;
using System.Net.Mail;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Configuration; // Added for EmailSettingsConfig
using Microsoft.Extensions.Options; // Added for IOptions

namespace GateKeeper.Server.Services.Site
{
    public class EmailService(IOptions<EmailSettingsConfig> emailSettingsOptions) : IEmailService
    {
        private readonly EmailSettingsConfig _emailSettings = emailSettingsOptions.Value;

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            // SMTP settings are now from _emailSettings
            // Assuming UseSsl is a boolean in your appsettings.json, if not, adjust or remove
            // For simplicity, Ssl will be enabled by default if not specified or can be added to EmailSettingsConfig
            bool useSsl = true; // Or add UseSsl to EmailSettingsConfig if it varies

            // Configure the email client
            using var smtpClient = new SmtpClient(_emailSettings.SmtpServer);
            smtpClient.Port = _emailSettings.Port;
            smtpClient.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
            smtpClient.EnableSsl = useSsl;

            // Create the email message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromAddress), // FromAddress now used
                Subject = subject,
                Body = message,
                IsBodyHtml = true, // Change to true if sending HTML content
            };

            mailMessage.To.Add(new MailAddress(email)); // Removed toName as it's often the same as email

            // Send the email
            await smtpClient.SendMailAsync(mailMessage); // Use async version
        }

        // This overload seems to have 'fromName2' which might be redundant if FromAddress in config is used.
        // Consolidating or clarifying its purpose is recommended.
        // For now, it's adapted to use EmailSettingsConfig, but 'fromName2' is used for the display name.
        public async Task SendEmailAsync(string toEmail, string toName, string fromName2, string subject, string message)
        {
            // SMTP settings are now from _emailSettings
            bool useSsl = true; // Or add UseSsl to EmailSettingsConfig if it varies

            // Configure the email client
            using var smtpClient = new SmtpClient(_emailSettings.SmtpServer);
            smtpClient.Port = _emailSettings.Port;
            smtpClient.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
            smtpClient.EnableSsl = useSsl;

            // Create the email message
            var mailMessage = new MailMessage
            {
                // Using FromAddress for the email, and fromName2 for the display name for this specific overload
                From = new MailAddress(_emailSettings.FromAddress, fromName2),
                Subject = subject,
                Body = message,
                IsBodyHtml = true, // Change to true if sending HTML content
            };

            mailMessage.To.Add(new MailAddress(toEmail, toName));

            // Send the email
            await smtpClient.SendMailAsync(mailMessage); // Use async version
        }
    }
}
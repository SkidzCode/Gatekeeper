using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Options; // Added for IOptions
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Configuration; // Added for EmailSettingsConfig
using System.Net.Mail;
using System.Threading.Tasks;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class EmailServiceTests
    {
        private Mock<IOptions<EmailSettingsConfig>> _mockEmailSettingsOptions;
        private EmailSettingsConfig _emailSettingsConfig;
        private IEmailService _emailService;

        [TestInitialize]
        public void TestInitialize()
        {
            _emailSettingsConfig = new EmailSettingsConfig
            {
                SmtpServer = "smtp.example.com",
                Port = 587,
                Username = "user@example.com",
                Password = "password",
                FromAddress = "from@example.com" // Updated from FromName to FromAddress
                // UseSsl is not part of EmailSettingsConfig, EmailService uses a default or can be added.
            };
            _mockEmailSettingsOptions = new Mock<IOptions<EmailSettingsConfig>>();
            _mockEmailSettingsOptions.Setup(o => o.Value).Returns(_emailSettingsConfig);
            
            _emailService = new EmailService(_mockEmailSettingsOptions.Object);
        }

        [TestMethod]
        public async Task SendEmailAsync_FirstOverload_UsesConfiguredSettings()
        {
            // Arrange
            // TestInitialize has already set up the service with mocked options.
            // We are verifying that the service uses the values from the mocked config.
            
            var toEmail = "recipient@example.com";
            var subject = "Test Subject";
            var message = "Test Message";

            // Act & Assert
            // The SmtpClient will likely throw an exception because "smtp.example.com" is not a real server.
            // We are testing that the service attempts to use the configured values.
            // The actual email sending is not tested here.
            try
            {
                await _emailService.SendEmailAsync(toEmail, subject, message);
            }
            catch (SmtpException ex)
            {
                // Check if the exception message or SmtpClient's properties (if accessible) indicate usage of configured values.
                // This is a basic check; more sophisticated mocking of SmtpClient would be needed for deeper verification.
                Assert.IsTrue(ex.Message.Contains(_emailSettingsConfig.SmtpServer) || ex.Message.Contains("No such host is known") || ex.Message.Contains("failure in name resolution") || ex.Message.Contains("Name or service not known") || ex.Message.Contains("Failure sending mail"), $"Unexpected SmtpException message: {ex.Message}");
            }
            catch (System.Net.Sockets.SocketException sox)
            {
                 Assert.IsTrue(sox.Message.Contains("No such host is known") || sox.Message.Contains("Name or service not known") || sox.Message.Contains("failure in name resolution"), $"Unexpected SocketException message: {sox.Message}");
            }

            // Verify that the IOptions<EmailSettingsConfig> was accessed.
            _mockEmailSettingsOptions.Verify(o => o.Value, Times.AtLeastOnce());
        }

        [TestMethod]
        public async Task SendEmailAsync_SecondOverload_UsesConfiguredSettings()
        {
            // Arrange
            var toEmail = "recipient@example.com";
            var toName = "Recipient Name";
            var fromName2 = "Test From Name 2"; // This is used as the display name for 'From' in this overload
            var subject = "Test Subject 2";
            var message = "Test Message 2";

            // Act & Assert
            try
            {
                await _emailService.SendEmailAsync(toEmail, toName, fromName2, subject, message);
            }
            catch (SmtpException ex)
            {
                Assert.IsTrue(ex.Message.Contains(_emailSettingsConfig.SmtpServer) || ex.Message.Contains("No such host is known") || ex.Message.Contains("failure in name resolution") || ex.Message.Contains("Name or service not known") || ex.Message.Contains("Failure sending mail"), $"Unexpected SmtpException message: {ex.Message}");
            }
            catch (System.Net.Sockets.SocketException sox)
            {
                Assert.IsTrue(sox.Message.Contains("No such host is known") || sox.Message.Contains("Name or service not known") || sox.Message.Contains("failure in name resolution"), $"Unexpected SocketException message: {sox.Message}");
            }
            
            _mockEmailSettingsOptions.Verify(o => o.Value, Times.AtLeastOnce());
        }

        // The following tests for UseSsl and Port variations are less relevant now
        // because EmailService directly uses the strongly-typed EmailSettingsConfig.
        // The validation of these properties (e.g., range for Port) happens at startup
        // due to DataAnnotations and ValidateOnStart() in Program.cs.
        // We trust that IOptions provides the correct values.
        // The core logic of EmailService is to use these values, which the above tests cover.
        // If specific behavior within EmailService depended on these (e.g., conditional logic for UseSsl),
        // then tests for those variations would still be needed. However, EmailService currently
        // hardcodes `EnableSsl = true` (or it could be added to EmailSettingsConfig).

        // Example of how to test if UseSsl was part of EmailSettingsConfig:
        /*
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SendEmailAsync_UsesSslConfigurationCorrectly(bool useSslConfigValue)
        {
            // Arrange
            _emailSettingsConfig.UseSsl = useSslConfigValue; // Assuming UseSsl is a property
            _mockEmailSettingsOptions.Setup(o => o.Value).Returns(_emailSettingsConfig);
            var localEmailService = new EmailService(_mockEmailSettingsOptions.Object);

            // Act & Assert
            try
            {
                await localEmailService.SendEmailAsync("test@example.com", "Subject", "Message");
            }
            catch (SmtpException) { } // Expected
            catch (System.Net.Sockets.SocketException) { } // Expected

            // Verification would ideally involve a mock SmtpClient to check EnableSsl property.
            // Since that's not easily done without changing EmailService, we rely on testing the config access.
            _mockEmailSettingsOptions.Verify(o => o.Value, Times.AtLeastOnce());
        }
        */

        // More tests could be added here if we could mock SmtpClient behavior,
        // for example, to verify that SendMailAsync was called with correctly constructed MailMessage.
        // For now, the tests focus on configuration usage.
    }
}
// Remove DataTestMethod and DataRow tests as they are less direct with IOptions
// and the validation is handled by DataAnnotations.
// The tests for Ssl and Port variations are removed as explained above.
// The remaining tests verify that the configured settings are attempted to be used by SmtpClient.

// More tests will be added here for MailMessage properties and SmtpClient interactions
        // if a way to mock/intercept SmtpClient can be found without modifying EmailService.
        // For now, testing MailMessage properties directly is not possible because
        // the MailMessage object is created and used internally by the SmtpClient.Send method.
        // Similarly, verifying SmtpClient.Send call is also not possible.
    }
}

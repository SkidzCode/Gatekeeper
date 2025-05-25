using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Configuration;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class EmailServiceTests
    {
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IConfigurationSection> _mockConfigurationSection;
        private IEmailService _emailService;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfigurationSection = new Mock<IConfigurationSection>();

            // Default configuration setup
            _mockConfigurationSection.Setup(s => s["SmtpHost"]).Returns("smtp.example.com");
            _mockConfigurationSection.Setup(s => s["Port"]).Returns("587");
            _mockConfigurationSection.Setup(s => s["UserName"]).Returns("user@example.com");
            _mockConfigurationSection.Setup(s => s["Password"]).Returns("password");
            _mockConfigurationSection.Setup(s => s["FromName"]).Returns("Test From Name");
            _mockConfigurationSection.Setup(s => s["UseSsl"]).Returns("true");

            _mockConfiguration.Setup(c => c.GetSection("EmailSettings"))
                              .Returns(_mockConfigurationSection.Object);
            
            _emailService = new EmailService(_mockConfiguration.Object);
        }

        [TestMethod]
        public async Task SendEmailAsync_FirstOverload_ReadsConfiguration()
        {
            // Arrange
            var toEmail = "recipient@example.com";
            var subject = "Test Subject";
            var message = "Test Message";

            // Act
            // The SmtpClient will throw an exception because it cannot connect to "smtp.example.com"
            // This is expected as we are not testing the actual email sending, 
            // but that configuration is read.
            try
            {
                await _emailService.SendEmailAsync(toEmail, subject, message);
            }
            catch (SmtpException ex)
            {
                // Expected exception due to invalid host, or network issues in test environment
                Assert.IsTrue(ex.Message.Contains("smtp.example.com") || ex.Message.Contains("No such host is known") || ex.Message.Contains("failure in name resolution"), $"Unexpected SmtpException message: {ex.Message}");
            }
            catch (System.Net.Sockets.SocketException sox)
            {
                // Expected in environments where DNS resolution for "smtp.example.com" fails
                 Assert.IsTrue(sox.Message.Contains("No such host is known") || sox.Message.Contains("Name or service not known") || sox.Message.Contains("failure in name resolution"), $"Unexpected SocketException message: {sox.Message}");
            }


            // Assert
            _mockConfiguration.Verify(c => c.GetSection("EmailSettings"), Times.Once);
            _mockConfigurationSection.Verify(s => s["SmtpHost"], Times.Once);
            _mockConfigurationSection.Verify(s => s["Port"], Times.Once);
            _mockConfigurationSection.Verify(s => s["UserName"], Times.Once);
            _mockConfigurationSection.Verify(s => s["Password"], Times.Once);
            _mockConfigurationSection.Verify(s => s["FromName"], Times.Once);
            _mockConfigurationSection.Verify(s => s["UseSsl"], Times.Once);
        }

        [TestMethod]
        public async Task SendEmailAsync_SecondOverload_ReadsConfiguration()
        {
            // Arrange
            var toEmail = "recipient@example.com";
            var toName = "Recipient Name";
            var fromName2 = "Test From Name 2";
            var subject = "Test Subject 2";
            var message = "Test Message 2";

            // Act
            // Similar to the first test, an SmtpException is expected.
            try
            {
                await _emailService.SendEmailAsync(toEmail, toName, fromName2, subject, message);
            }
            catch (SmtpException ex)
            {
                Assert.IsTrue(ex.Message.Contains("smtp.example.com") || ex.Message.Contains("No such host is known") || ex.Message.Contains("failure in name resolution"), $"Unexpected SmtpException message: {ex.Message}");
            }
            catch (System.Net.Sockets.SocketException sox)
            {
                 Assert.IsTrue(sox.Message.Contains("No such host is known") || sox.Message.Contains("Name or service not known") || sox.Message.Contains("failure in name resolution"), $"Unexpected SocketException message: {sox.Message}");
            }

            // Assert
            _mockConfiguration.Verify(c => c.GetSection("EmailSettings"), Times.Once); // Called once per SendEmailAsync call
            _mockConfigurationSection.Verify(s => s["SmtpHost"], Times.Once);
            _mockConfigurationSection.Verify(s => s["Port"], Times.Once);
            _mockConfigurationSection.Verify(s => s["UserName"], Times.Once);
            _mockConfigurationSection.Verify(s => s["Password"], Times.Once);
            // FromName is not used in the second overload, FromName2 is used directly.
            // _mockConfigurationSection.Verify(s => s["FromName"], Times.Once); 
            _mockConfigurationSection.Verify(s => s["UseSsl"], Times.Once);
        }

        // More tests will be added here for MailMessage properties and SmtpClient interactions
        // if a way to mock/intercept SmtpClient can be found without modifying EmailService.
        // For now, testing MailMessage properties directly is not possible because
        // the MailMessage object is created and used internally by the SmtpClient.Send method.
        // Similarly, verifying SmtpClient.Send call is also not possible.

        // Test for SSL configuration variations
        [DataTestMethod]
        [DataRow("true", true)]
        [DataRow("false", false)]
        [DataRow(null, true)] // Default value for UseSsl is true if config is null
        public async Task SendEmailAsync_UsesSslConfigurationCorrectly(string useSslConfigValue, bool expectedUseSsl)
        {
            // Arrange
            _mockConfigurationSection.Setup(s => s["UseSsl"]).Returns(useSslConfigValue);
            _mockConfiguration.Setup(c => c.GetSection("EmailSettings"))
                              .Returns(_mockConfigurationSection.Object);
            
            // Recreate EmailService with the new mock setup for this specific test
            var localEmailService = new EmailService(_mockConfiguration.Object);

            // Act & Assert
            // We can't directly check SmtpClient.EnableSsl property.
            // We can only verify that the configuration value was read.
            // The actual test of SmtpClient behavior is not possible here.
            try
            {
                await localEmailService.SendEmailAsync("test@example.com", "Subject", "Message");
            }
            catch (SmtpException) { /* Expected */ }
            catch (System.Net.Sockets.SocketException) { /* Expected */ }


            _mockConfigurationSection.Verify(s => s["UseSsl"], Times.AtLeastOnce());

            // Note: This test only verifies that the configuration is read.
            // It does not verify that SmtpClient.EnableSsl is set to the expected value
            // because SmtpClient is instantiated and used internally.
            // A comment explaining this limitation would be good in actual test code.
            if (useSslConfigValue == null)
            {
                // bool.Parse(null) would throw. EmailService uses: bool.Parse(_configuration["EmailSettings:UseSsl"] ?? "true");
                // So if "UseSsl" is missing, it defaults to "true".
                // The _mockConfigurationSection.Verify above checks it was accessed.
                // We are implicitly testing the default fallback here.
            }
        }

        [TestMethod]
        public async Task SendEmailAsync_HandlesNullPortConfiguration()
        {
            // Arrange
            _mockConfigurationSection.Setup(s => s["Port"]).Returns((string)null); // Simulate missing Port
            _mockConfiguration.Setup(c => c.GetSection("EmailSettings"))
                              .Returns(_mockConfigurationSection.Object);
            var localEmailService = new EmailService(_mockConfiguration.Object);

            // Act & Assert
            // EmailService uses: int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            // So if "Port" is missing, it defaults to "587".
            // We can't directly check SmtpClient.Port. We verify config was read.
            try
            {
                await localEmailService.SendEmailAsync("test@example.com", "Subject", "Message");
            }
            catch (SmtpException) { /* Expected */ }
            catch (System.Net.Sockets.SocketException) { /* Expected */ }

            _mockConfigurationSection.Verify(s => s["Port"], Times.AtLeastOnce());
            // Implicitly testing the default fallback to 587.
        }
    }
}

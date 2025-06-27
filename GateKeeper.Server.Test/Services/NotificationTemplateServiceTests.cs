using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using GateKeeper.Server.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging; // Added for ILogger
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq; // Keep for It.IsAny if still used, or general LINQ operations

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class NotificationTemplateServiceTests
    {
        private Mock<INotificationTemplateRepository> _mockTemplateRepository;
        private Mock<IOptions<LocalizationSettingsConfig>> _mockLocalizationSettingsConfig;
        private Mock<ILogger<NotificationTemplateService>> _mockLogger; // Mock for ILogger
        private NotificationTemplateService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockTemplateRepository = new Mock<INotificationTemplateRepository>();
            _mockLocalizationSettingsConfig = new Mock<IOptions<LocalizationSettingsConfig>>();
            _mockLogger = new Mock<ILogger<NotificationTemplateService>>(); // Initialize mock logger

            _mockLocalizationSettingsConfig.Setup(ap => ap.Value)
                .Returns(new LocalizationSettingsConfig { DefaultLanguageCode = "en-US" });

            _service = new NotificationTemplateService(
                _mockTemplateRepository.Object,
                _mockLocalizationSettingsConfig.Object,
                _mockLogger.Object); // Pass logger to constructor
        }

        private NotificationTemplate CreateTestTemplate(int id = 1, string name = "Test Template", string channel = "email",
                                                        string tokenType = "TestToken", string subject = "Test Subject",
                                                        string body = "Test Body", bool isActive = true)
        {
            return new NotificationTemplate
            {
                TemplateId = id, TemplateName = name, Channel = channel, TokenType = tokenType,
                Subject = subject, Body = body, IsActive = isActive,
                CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAt = DateTime.UtcNow
            };
        }

        private NotificationTemplateLocalization CreateTestLocalization(int templateId = 1, string languageCode = "es-ES",
                                                                        string? localizedSubject = "Spanish Subject", string localizedBody = "Spanish Body")
        {
            return new NotificationTemplateLocalization
            {
                TemplateId = templateId, LanguageCode = languageCode, LocalizedSubject = localizedSubject, LocalizedBody = localizedBody,
                CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAt = DateTime.UtcNow
            };
        }

        #region InsertNotificationTemplateAsync Tests
        [TestMethod]
        public async Task InsertNotificationTemplateAsync_CallsRepositoryAndReturnsId()
        {
            var template = CreateTestTemplate(id: 0);
            var expectedNewId = 123;
            _mockTemplateRepository.Setup(repo => repo.InsertNotificationTemplateAsync(template)).ReturnsAsync(expectedNewId);

            var result = await _service.InsertNotificationTemplateAsync(template);

            Assert.AreEqual(expectedNewId, result);
            _mockTemplateRepository.Verify(repo => repo.InsertNotificationTemplateAsync(template), Times.Once);
        }

        [TestMethod]
        public async Task InsertNotificationTemplateAsync_RepositoryThrowsException_ReturnsZeroAndLogsError()
        {
            var template = CreateTestTemplate(id:0);
            _mockTemplateRepository.Setup(repo => repo.InsertNotificationTemplateAsync(template))
                                   .ThrowsAsync(new Exception("DB error"));

            var result = await _service.InsertNotificationTemplateAsync(template);

            Assert.AreEqual(0, result);
             _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error inserting notification template: {template.TemplateName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
        #endregion

        #region UpdateNotificationTemplateAsync Tests
        [TestMethod]
        public async Task UpdateNotificationTemplateAsync_CallsRepository()
        {
            var template = CreateTestTemplate(id: 1);
            _mockTemplateRepository.Setup(repo => repo.UpdateNotificationTemplateAsync(template)).Returns(Task.CompletedTask);

            await _service.UpdateNotificationTemplateAsync(template);

            _mockTemplateRepository.Verify(repo => repo.UpdateNotificationTemplateAsync(template), Times.Once);
        }
        #endregion

        #region DeleteNotificationTemplateAsync Tests
        [TestMethod]
        public async Task DeleteNotificationTemplateAsync_CallsRepository()
        {
            var templateId = 123;
            _mockTemplateRepository.Setup(repo => repo.DeleteNotificationTemplateAsync(templateId)).Returns(Task.CompletedTask);

            await _service.DeleteNotificationTemplateAsync(templateId);

            _mockTemplateRepository.Verify(repo => repo.DeleteNotificationTemplateAsync(templateId), Times.Once);
        }
        #endregion

        #region GetNotificationTemplateByIdAsync Tests
        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_TemplateFound_NoLocalizationNeeded_ReturnsTemplate()
        {
            var templateId = 1;
            var templateData = CreateTestTemplate(id: templateId);
            _mockTemplateRepository.Setup(repo => repo.GetNotificationTemplateByIdAsync(templateId)).ReturnsAsync(templateData);
            // Assuming default language is "en-US" and no languageCode or "en-US" is passed

            var result = await _service.GetNotificationTemplateByIdAsync(templateId, "en-US");

            Assert.IsNotNull(result);
            Assert.AreEqual(templateData.TemplateId, result.TemplateId);
            _mockTemplateRepository.Verify(repo => repo.GetLocalizationAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_TemplateNotFound_ReturnsNull()
        {
            var templateId = 404;
            _mockTemplateRepository.Setup(repo => repo.GetNotificationTemplateByIdAsync(templateId)).ReturnsAsync((NotificationTemplate)null);

            var result = await _service.GetNotificationTemplateByIdAsync(templateId);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_SpecificLanguageExists_ReturnsLocalizedTemplate()
        {
            var templateId = 1;
            var defaultTemplate = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            var localizedData = CreateTestLocalization(templateId: templateId, languageCode: "es-ES", localizedSubject: "Spanish Subject", localizedBody: "Spanish Body");
            _mockTemplateRepository.Setup(repo => repo.GetNotificationTemplateByIdAsync(templateId)).ReturnsAsync(defaultTemplate);
            _mockTemplateRepository.Setup(repo => repo.GetLocalizationAsync(templateId, "es-ES")).ReturnsAsync(localizedData);

            var result = await _service.GetNotificationTemplateByIdAsync(templateId, "es-ES");

            Assert.IsNotNull(result);
            Assert.AreEqual(localizedData.LocalizedSubject, result.Subject);
            Assert.AreEqual(localizedData.LocalizedBody, result.Body);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_SpecificLanguageDoesNotExist_ReturnsDefaultTemplate()
        {
            var templateId = 1;
            var defaultTemplate = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            _mockTemplateRepository.Setup(repo => repo.GetNotificationTemplateByIdAsync(templateId)).ReturnsAsync(defaultTemplate);
            _mockTemplateRepository.Setup(repo => repo.GetLocalizationAsync(templateId, "fr-FR")).ReturnsAsync((NotificationTemplateLocalization)null);

            var result = await _service.GetNotificationTemplateByIdAsync(templateId, "fr-FR");

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplate.Subject, result.Subject);
            Assert.AreEqual(defaultTemplate.Body, result.Body);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_LocalizedSubjectIsNull_UsesDefaultSubject()
        {
            var templateId = 1;
            var defaultTemplate = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            var localizedData = CreateTestLocalization(templateId: templateId, languageCode: "es-ES", localizedSubject: null, localizedBody: "Spanish Body");
             _mockTemplateRepository.Setup(repo => repo.GetNotificationTemplateByIdAsync(templateId)).ReturnsAsync(defaultTemplate);
            _mockTemplateRepository.Setup(repo => repo.GetLocalizationAsync(templateId, "es-ES")).ReturnsAsync(localizedData);

            var result = await _service.GetNotificationTemplateByIdAsync(templateId, "es-ES");

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplate.Subject, result.Subject); // Fallback to default
            Assert.AreEqual(localizedData.LocalizedBody, result.Body);
        }

        #endregion

        #region GetNotificationTemplateByNameAsync Tests
        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_TemplateFound_DefaultLang_ReturnsTemplate()
        {
            var templateName = "TestTemplate";
            var templateData = CreateTestTemplate(name: templateName);
            _mockTemplateRepository.Setup(repo => repo.GetNotificationTemplateByNameAsync(templateName)).ReturnsAsync(templateData);
            // Default language "en-US", requesting null or "en-US" for languageCode

            var result = await _service.GetNotificationTemplateByNameAsync(templateName, null);

            Assert.IsNotNull(result);
            Assert.AreEqual(templateData.TemplateName, result.TemplateName);
            _mockTemplateRepository.Verify(repo => repo.GetLocalizationAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_TemplateNotFound_ReturnsNull()
        {
            var templateName = "NonExistent";
            _mockTemplateRepository.Setup(repo => repo.GetNotificationTemplateByNameAsync(templateName)).ReturnsAsync((NotificationTemplate)null);

            var result = await _service.GetNotificationTemplateByNameAsync(templateName);
            Assert.IsNull(result);
        }


        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_SpecificLanguageExists_ReturnsLocalizedTemplate()
        {
            var templateName = "ByNameTest";
            var templateId = 7;
            var defaultTemplate = CreateTestTemplate(id: templateId, name: templateName, subject: "Default Subject", body: "Default Body");
            var localizedData = CreateTestLocalization(templateId: templateId, languageCode: "de-DE", localizedSubject: "German Subject", localizedBody: "German Body");
            _mockTemplateRepository.Setup(repo => repo.GetNotificationTemplateByNameAsync(templateName)).ReturnsAsync(defaultTemplate);
            _mockTemplateRepository.Setup(repo => repo.GetLocalizationAsync(templateId, "de-DE")).ReturnsAsync(localizedData);

            var result = await _service.GetNotificationTemplateByNameAsync(templateName, "de-DE");

            Assert.IsNotNull(result);
            Assert.AreEqual(localizedData.LocalizedSubject, result.Subject);
            Assert.AreEqual(localizedData.LocalizedBody, result.Body);
        }

        #endregion

        #region GetAllNotificationTemplatesAsync Tests
        [TestMethod]
        public async Task GetAllNotificationTemplatesAsync_TemplatesFound_ReturnsListOfTemplates()
        {
            var templatesData = new List<NotificationTemplate> { CreateTestTemplate(1), CreateTestTemplate(2) };
            _mockTemplateRepository.Setup(repo => repo.GetAllNotificationTemplatesAsync()).ReturnsAsync(templatesData);

            var result = await _service.GetAllNotificationTemplatesAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(templatesData.Count, result.Count);
            _mockTemplateRepository.Verify(repo => repo.GetAllNotificationTemplatesAsync(), Times.Once);
        }
        #endregion
    }
}
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using GateKeeper.Server.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Data;
using MySqlConnector;
using System.Linq;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class NotificationTemplateServiceTests
    {
        private Mock<IDbHelper> _mockDbHelper;
        private Mock<IMySqlConnectorWrapper> _mockMySqlConnectorWrapper;
        private Mock<IMySqlDataReaderWrapper> _mockDataReader;
        private Mock<IOptions<LocalizationSettingsConfig>> _mockLocalizationSettingsConfig;
        private NotificationTemplateService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockDbHelper = new Mock<IDbHelper>();
            _mockMySqlConnectorWrapper = new Mock<IMySqlConnectorWrapper>();
            _mockDataReader = new Mock<IMySqlDataReaderWrapper>();
            _mockLocalizationSettingsConfig = new Mock<IOptions<LocalizationSettingsConfig>>();

            _mockLocalizationSettingsConfig.Setup(ap => ap.Value).Returns(new LocalizationSettingsConfig { DefaultLanguageCode = "en-US" });

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));
            // It's good practice to ensure the wrapper is set up for OpenConnectionAsync if the SUT might call it, though for these tests it might not be strictly necessary
            // _mockMySqlConnectorWrapper.Setup(c => c.OpenConnectionAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));
            _service = new NotificationTemplateService(_mockDbHelper.Object, _mockLocalizationSettingsConfig.Object);
        }

        private NotificationTemplate CreateTestTemplate(int id = 1, string name = "Test Template", string channel = "email",
                                                        string tokenType = "TestToken", string subject = "Test Subject",
                                                        string body = "Test Body", bool isActive = true,
                                                        DateTime? createdAt = null, DateTime? updatedAt = null)
        {
            return new NotificationTemplate
            {
                TemplateId = id,
                TemplateName = name,
                Channel = channel,
                TokenType = tokenType,
                Subject = subject,
                Body = body,
                IsActive = isActive,
                CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-1),
                UpdatedAt = updatedAt ?? DateTime.UtcNow
            };
        }

        private void SetupMockReaderForTemplate(NotificationTemplate template)
        {
            _mockDataReader.Setup(r => r.GetInt32("TemplateId")).Returns(template.TemplateId);
            _mockDataReader.Setup(r => r.GetString("TemplateName")).Returns(template.TemplateName);
            _mockDataReader.Setup(r => r.GetString("channel")).Returns(template.Channel); // Lowercase as in SUT
            _mockDataReader.Setup(r => r.GetString("TokenType")).Returns(template.TokenType);
            _mockDataReader.Setup(r => r.GetString("subject")).Returns(template.Subject); // Lowercase
            _mockDataReader.Setup(r => r.GetString("body")).Returns(template.Body);       // Lowercase
            _mockDataReader.Setup(r => r.GetInt32("IsActive")).Returns(template.IsActive ? 1 : 0);
            _mockDataReader.Setup(r => r.GetDateTime("CreatedAt")).Returns(template.CreatedAt);
            _mockDataReader.Setup(r => r.GetDateTime("UpdatedAt")).Returns(template.UpdatedAt);
        }

        private NotificationTemplateLocalization CreateTestLocalization(int localizationId = 1, int templateId = 1, string languageCode = "es-ES",
                                                                    string? localizedSubject = "Spanish Subject", string localizedBody = "Spanish Body",
                                                                    DateTime? createdAt = null, DateTime? updatedAt = null)
        {
            return new NotificationTemplateLocalization
            {
                LocalizationId = localizationId,
                TemplateId = templateId,
                LanguageCode = languageCode,
                LocalizedSubject = localizedSubject,
                LocalizedBody = localizedBody,
                CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-1),
                UpdatedAt = updatedAt ?? DateTime.UtcNow
            };
        }

        private void SetupMockReaderForLocalization(NotificationTemplateLocalization localization)
        {
            _mockDataReader.Setup(r => r.GetInt32("LocalizationId")).Returns(localization.LocalizationId);
            _mockDataReader.Setup(r => r.GetInt32("TemplateId")).Returns(localization.TemplateId);
            _mockDataReader.Setup(r => r.GetString("LanguageCode")).Returns(localization.LanguageCode);
            _mockDataReader.Setup(r => r.IsDBNull(It.Is<string>(s => s == "LocalizedSubject"))).Returns(localization.LocalizedSubject == null);
            if (localization.LocalizedSubject != null)
            {
                _mockDataReader.Setup(r => r.GetString("LocalizedSubject")).Returns(localization.LocalizedSubject);
            }
            else // Ensure GetString is not called or returns default if IsDBNull was true. Depending on MySqlDataReader behavior, this might not be strictly needed if IsDBNull check in SUT is robust.
            {
                _mockDataReader.Setup(r => r.GetString("LocalizedSubject")).Returns(default(string));
            }
            _mockDataReader.Setup(r => r.GetString("LocalizedBody")).Returns(localization.LocalizedBody);
            _mockDataReader.Setup(r => r.GetDateTime("CreatedAt")).Returns(localization.CreatedAt);
            _mockDataReader.Setup(r => r.GetDateTime("UpdatedAt")).Returns(localization.UpdatedAt);
        }


        #region InsertNotificationTemplateAsync Tests
        [TestMethod]
        public async Task InsertNotificationTemplateAsync_CallsSPAndReturnsNewId()
        {
            // Arrange
            var template = CreateTestTemplate(id: 0); // ID is auto-generated
            var expectedNewId = 123;
            var outputParams = new Dictionary<string, object> { { "NewTemplateId", expectedNewId.ToString() } }; // SUT parses string

            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteNonQueryWithOutputAsync(
                    "NotificationTemplateInsert",
                    CommandType.StoredProcedure,
                    It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(outputParams)
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) =>
                {
                    Assert.AreEqual("NotificationTemplateInsert", proc);
                    Assert.AreEqual(template.TemplateName, pars.First(p => p.ParameterName == "@p_TemplateName").Value);
                    Assert.AreEqual(template.Channel, pars.First(p => p.ParameterName == "@p_Channel").Value);
                    Assert.AreEqual(template.TokenType, pars.First(p => p.ParameterName == "@p_TokenType").Value);
                    Assert.AreEqual(template.Subject, pars.First(p => p.ParameterName == "@p_Subject").Value);
                    Assert.AreEqual(template.Body, pars.First(p => p.ParameterName == "@p_Body").Value);
                    Assert.AreEqual(template.IsActive, pars.First(p => p.ParameterName == "@p_IsActive").Value);
                });

            // Act
            var result = await _service.InsertNotificationTemplateAsync(template);

            // Assert
            Assert.AreEqual(expectedNewId, result);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryWithOutputAsync("NotificationTemplateInsert", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }
        
        [TestMethod]
        public async Task InsertNotificationTemplateAsync_OutputParamNotInt_ReturnsZero()
        {
            var template = CreateTestTemplate(id: 0);
            var outputParams = new Dictionary<string, object> { { "NewTemplateId", "not-an-int" } };

            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteNonQueryWithOutputAsync("NotificationTemplateInsert", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(outputParams);
            
            var result = await _service.InsertNotificationTemplateAsync(template);
            
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public async Task InsertNotificationTemplateAsync_OutputParamMissing_ReturnsZero()
        {
            var template = CreateTestTemplate(id: 0);
            var outputParams = new Dictionary<string, object>(); // Missing "NewTemplateId"

            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteNonQueryWithOutputAsync("NotificationTemplateInsert", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(outputParams);
            
            var result = await _service.InsertNotificationTemplateAsync(template);
            
            Assert.AreEqual(0, result);
        }
        #endregion

        #region UpdateNotificationTemplateAsync Tests
        [TestMethod]
        public async Task UpdateNotificationTemplateAsync_CallsSPWithCorrectParameters()
        {
            // Arrange
            var template = CreateTestTemplate(id: 1);

            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteNonQueryAsync(
                    "NotificationTemplateUpdate",
                    CommandType.StoredProcedure,
                    It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(1) // Simulate 1 row affected
                .Callback<string, CommandType, MySqlParameter[]>((proc, type, pars) =>
                {
                    Assert.AreEqual("NotificationTemplateUpdate", proc);
                    Assert.AreEqual(template.TemplateId, pars.First(p => p.ParameterName == "@p_TemplateId").Value);
                    Assert.AreEqual(template.TemplateName, pars.First(p => p.ParameterName == "@p_TemplateName").Value);
                    // ... other parameters can be checked similarly
                });

            // Act
            await _service.UpdateNotificationTemplateAsync(template);

            // Assert
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync("NotificationTemplateUpdate", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }
        #endregion

        #region DeleteNotificationTemplateAsync Tests
        [TestMethod]
        public async Task DeleteNotificationTemplateAsync_CallsSPWithCorrectParameters()
        {
            // Arrange
            var templateId = 123;

            _mockMySqlConnectorWrapper
                .Setup(c => c.ExecuteNonQueryAsync(
                    "NotificationTemplateDelete",
                    CommandType.StoredProcedure,
                    It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId")))
                .ReturnsAsync(1); // Simulate 1 row affected

            // Act
            await _service.DeleteNotificationTemplateAsync(templateId);

            // Assert
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync("NotificationTemplateDelete", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId)), Times.Once);
        }
        #endregion

        #region GetNotificationTemplateByIdAsync Tests
        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_TemplateFound_ReturnsTemplate()
        {
            // Arrange
            var templateId = 1;
            var template = CreateTestTemplate(id: templateId);
            
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", // As per SUT
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId")))
                .ReturnsAsync(_mockDataReader.Object);

            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplate(template);

            // Act
            var result = await _service.GetNotificationTemplateByIdAsync(templateId, null); // Pass null for languageCode

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(template.TemplateId, result.TemplateId);
            Assert.AreEqual(template.TemplateName, result.TemplateName);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_TemplateNotFound_ReturnsNull()
        {
            // Arrange
            var templateId = 404;
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", 
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId)))
                .ReturnsAsync(_mockDataReader.Object);

            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false);

            // Act
            var result = await _service.GetNotificationTemplateByIdAsync(templateId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_SpecificLanguageExists_ReturnsLocalizedTemplate()
        {
            // Arrange
            var templateId = 1;
            var defaultTemplate = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            var localizedTemplateData = CreateTestLocalization(templateId: templateId, languageCode: "es-ES", localizedSubject: "Spanish Subject", localizedBody: "Spanish Body");
            var languageToRequest = "es-ES";

            // Mock for NotificationTemplateGet (default template)
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet",
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && p[0].ParameterName == "@p_TemplateId" && (int)p[0].Value == templateId)))
                .ReturnsAsync(_mockDataReader.Object)
                .Verifiable("NotificationTemplateGet was not called or called with wrong parameters.");
            
            // Mock for NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode",
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 2 && 
                                             p[0].ParameterName == "@p_TemplateId" && (int)p[0].Value == templateId &&
                                             p[1].ParameterName == "@p_LanguageCode" && (string)p[1].Value == languageToRequest)))
                .ReturnsAsync(_mockDataReader.Object) // Use a new mock reader instance for the second call
                .Verifiable("Localization SP was not called or called with wrong parameters.");

            // Setup reader for the first call (default template)
            _mockDataReader.SetupSequence(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(true)  // For default template
                           .ReturnsAsync(true); // For localized template

            // Setup mock reader for default template then for localization
            _mockDataReader.SetupSequence(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(true)  // For default template
                           .Callback(() => SetupMockReaderForTemplate(defaultTemplate))
                           .ReturnsAsync(true)  // For localized template
                           .Callback(() => SetupMockReaderForLocalization(localizedTemplateData));

            // Act
            var result = await _service.GetNotificationTemplateByIdAsync(templateId, languageToRequest);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(localizedTemplateData.LocalizedSubject, result.Subject);
            Assert.AreEqual(localizedTemplateData.LocalizedBody, result.Body);
            _mockMySqlConnectorWrapper.Verify(); // Verify all verifiable setups
        }


        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_SpecificLanguageDoesNotExist_ReturnsDefaultTemplate()
        {
            var templateId = 1;
            var defaultTemplate = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            var languageToRequest = "fr-FR"; // Non-existent localization

            // Mock for NotificationTemplateGet
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId)))
                .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true); // Simulate found
            SetupMockReaderForTemplate(defaultTemplate);

            // Mock for NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode - not found
            var mockLocalizationReader = new Mock<IMySqlDataReaderWrapper>();
            mockLocalizationReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false); // Simulate not found

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", CommandType.StoredProcedure,
                 It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId && (string)p[1].Value == languageToRequest)))
                .ReturnsAsync(mockLocalizationReader.Object);
            
            var result = await _service.GetNotificationTemplateByIdAsync(templateId, languageToRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplate.Subject, result.Subject);
            Assert.AreEqual(defaultTemplate.Body, result.Body);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_LanguageCodeIsNull_UsesDefaultLanguageAndReturnsDefaultTemplate()
        {
            var templateId = 1;
            var defaultTemplate = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId)))
                .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplate(defaultTemplate);
            
            var result = await _service.GetNotificationTemplateByIdAsync(templateId, null);

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplate.Subject, result.Subject);
            Assert.AreEqual(defaultTemplate.Body, result.Body);
            // Verify localization SP was NOT called
             _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Never);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_LanguageCodeIsDefault_ReturnsDefaultTemplateAndSkipsLocalizationCall()
        {
            var templateId = 1;
            var defaultTemplate = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            _mockLocalizationSettingsConfig.Setup(ap => ap.Value).Returns(new LocalizationSettingsConfig { DefaultLanguageCode = "en-US" });


            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId)))
                .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplate(defaultTemplate);

            var result = await _service.GetNotificationTemplateByIdAsync(templateId, "en-US"); // Requesting default language

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplate.Subject, result.Subject);
            Assert.AreEqual(defaultTemplate.Body, result.Body);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Never);
        }
        
        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_LocalizedSubjectIsNull_UsesDefaultSubject()
        {
            var templateId = 1;
            var defaultTemplate = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            var localizedData = CreateTestLocalization(templateId: templateId, languageCode: "es-ES", localizedSubject: null, localizedBody: "Spanish Body");
            var languageToRequest = "es-ES";

            // Mock for NotificationTemplateGet
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId)))
                .ReturnsAsync(_mockDataReader.Object);

            // Mock for NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode
             var mockLocalizationReader = new Mock<IMySqlDataReaderWrapper>();
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId && (string)p[1].Value == languageToRequest)))
                .ReturnsAsync(mockLocalizationReader.Object);

            // Setup reader sequence: first for default, then for localization
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true); // Default template found
            SetupMockReaderForTemplate(defaultTemplate); // Setup for default template

            mockLocalizationReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true); // Localization found
            SetupMockReaderForLocalization(localizedData); // Setup for localization on the second reader

            var result = await _service.GetNotificationTemplateByIdAsync(templateId, languageToRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplate.Subject, result.Subject); // Fallback to default subject
            Assert.AreEqual(localizedData.LocalizedBody, result.Body);
        }

        #endregion

        #region GetNotificationTemplateByNameAsync Tests
        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_TemplateFound_ReturnsTemplate_DefaultLang()
        {
            // Arrange
            var templateName = "Test Template";
            var template = CreateTestTemplate(name: templateName);
            
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGetByName", 
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (string)p[0].Value == templateName && p[0].ParameterName == "@p_TemplateName")))
                .ReturnsAsync(_mockDataReader.Object);

            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplate(template);
            
            // Act
            var result = await _service.GetNotificationTemplateByNameAsync(templateName, null); // Default language

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(template.TemplateName, result.TemplateName);
            Assert.AreEqual(template.Subject, result.Subject); // Should be default subject
             _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Never);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_TemplateNotFound_ReturnsNull()
        {
            // Arrange
            var templateName = "NonExistent";
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGetByName", 
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (string)p[0].Value == templateName)))
                .ReturnsAsync(_mockDataReader.Object);

            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false);

            // Act
            var result = await _service.GetNotificationTemplateByNameAsync(templateName, null);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_SpecificLanguageExists_ReturnsLocalizedTemplate()
        {
            var templateName = "TestName";
            var templateId = 5;
            var defaultTemplate = CreateTestTemplate(id: templateId, name: templateName, subject: "Default Subject", body: "Default Body");
            var localizedData = CreateTestLocalization(templateId: templateId, languageCode: "es-ES", localizedSubject: "Spanish Subject", localizedBody: "Spanish Body");
            var languageToRequest = "es-ES";

            // Mock for NotificationTemplateGetByName
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGetByName", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (string)p[0].Value == templateName)))
                .ReturnsAsync(_mockDataReader.Object);
            
            // Mock for NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode
            var mockLocalizationReader = new Mock<IMySqlDataReaderWrapper>();
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId && (string)p[1].Value == languageToRequest)))
                .ReturnsAsync(mockLocalizationReader.Object);

            // Setup reader sequence
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true); // Default template found
            SetupMockReaderForTemplate(defaultTemplate); 

            mockLocalizationReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true); // Localization found
            SetupMockReaderForLocalization(localizedData);


            var result = await _service.GetNotificationTemplateByNameAsync(templateName, languageToRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual(localizedData.LocalizedSubject, result.Subject);
            Assert.AreEqual(localizedData.LocalizedBody, result.Body);
        }


        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_SpecificLanguageDoesNotExist_ReturnsDefaultTemplate()
        {
            var templateName = "TestName";
            var templateId = 6;
            var defaultTemplate = CreateTestTemplate(id: templateId, name: templateName, subject: "Default Subject", body: "Default Body");
            var languageToRequest = "fr-FR";

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGetByName", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (string)p[0].Value == templateName)))
                .ReturnsAsync(_mockDataReader.Object);
             SetupMockReaderForTemplate(defaultTemplate); // Setup for default template
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);


            var mockLocalizationReader = new Mock<IMySqlDataReaderWrapper>();
            mockLocalizationReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false); // Localization not found
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => (int)p[0].Value == templateId && (string)p[1].Value == languageToRequest)))
                .ReturnsAsync(mockLocalizationReader.Object);

            var result = await _service.GetNotificationTemplateByNameAsync(templateName, languageToRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplate.Subject, result.Subject);
            Assert.AreEqual(defaultTemplate.Body, result.Body);
        }

        #endregion

        #region GetAllNotificationTemplatesAsync Tests
        [TestMethod]
        public async Task GetAllNotificationTemplatesAsync_TemplatesFound_ReturnsListOfTemplates()
        {
            // Arrange
            var templatesData = new List<NotificationTemplate>
            {
                CreateTestTemplate(1, name: "Template 1"),
                CreateTestTemplate(2, name: "Template 2")
            };

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("NotificationTemplateGetAll", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            
            var readCallCount = 0;
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < templatesData.Count)
                           .Callback(() => 
                           {
                               if (readCallCount < templatesData.Count)
                               {
                                   SetupMockReaderForTemplate(templatesData[readCallCount]);
                               }
                               readCallCount++;
                           });
            
            // Act
            var result = await _service.GetAllNotificationTemplatesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(templatesData.Count, result.Count);
            for(int i=0; i<templatesData.Count; i++)
            {
                Assert.AreEqual(templatesData[i].TemplateName, result[i].TemplateName);
            }
        }

        [TestMethod]
        public async Task GetAllNotificationTemplatesAsync_NoTemplatesFound_ReturnsEmptyList()
        {
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("NotificationTemplateGetAll", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false);

            var result = await _service.GetAllNotificationTemplatesAsync();
            
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
        #endregion
    }
}

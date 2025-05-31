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
using System.Threading; // Added for CancellationToken

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class NotificationTemplateServiceTests
    {
        private Mock<IDbHelper> _mockDbHelper;
        private Mock<IMySqlConnectorWrapper> _mockMySqlConnectorWrapper;
        // _mockDataReader is no longer a class-level field, as tests will create specific instances.
        private Mock<IOptions<LocalizationSettingsConfig>> _mockLocalizationSettingsConfig;
        private NotificationTemplateService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockDbHelper = new Mock<IDbHelper>();
            _mockMySqlConnectorWrapper = new Mock<IMySqlConnectorWrapper>();
            _mockLocalizationSettingsConfig = new Mock<IOptions<LocalizationSettingsConfig>>();

            _mockLocalizationSettingsConfig.Setup(ap => ap.Value).Returns(new LocalizationSettingsConfig { DefaultLanguageCode = "en-US" });

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));
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

        // Helper to set up a mock reader for NotificationTemplate data
        // IMPORTANT: Verify and adjust ordinal values (0, 1, 2, etc.) to match your SP's column order!
        private void SetupMockReaderForTemplateData(Mock<IMySqlDataReaderWrapper> mockReader, NotificationTemplate template)
        {
            mockReader.Setup(r => r.GetOrdinal("TemplateId")).Returns(0);
            mockReader.Setup(r => r.GetOrdinal("TemplateName")).Returns(1);
            mockReader.Setup(r => r.GetOrdinal("channel")).Returns(2);
            mockReader.Setup(r => r.GetOrdinal("TokenType")).Returns(3);
            mockReader.Setup(r => r.GetOrdinal("subject")).Returns(4);
            mockReader.Setup(r => r.GetOrdinal("body")).Returns(5);
            mockReader.Setup(r => r.GetOrdinal("IsActive")).Returns(6);
            mockReader.Setup(r => r.GetOrdinal("CreatedAt")).Returns(7);
            mockReader.Setup(r => r.GetOrdinal("UpdatedAt")).Returns(8);

            mockReader.Setup(r => r.GetInt32("TemplateId")).Returns(template.TemplateId);
            mockReader.Setup(r => r.GetString("TemplateName")).Returns(template.TemplateName);
            mockReader.Setup(r => r.GetString("channel")).Returns(template.Channel);
            mockReader.Setup(r => r.GetString("TokenType")).Returns(template.TokenType);
            mockReader.Setup(r => r.GetString("subject")).Returns(template.Subject);
            mockReader.Setup(r => r.GetString("body")).Returns(template.Body);
            mockReader.Setup(r => r.GetInt32("IsActive")).Returns(template.IsActive ? 1 : 0);
            mockReader.Setup(r => r.GetDateTime("CreatedAt")).Returns(template.CreatedAt);
            mockReader.Setup(r => r.GetDateTime("UpdatedAt")).Returns(template.UpdatedAt);
        }

        // Helper to set up a mock reader for NotificationTemplateLocalization data
        // IMPORTANT: Verify and adjust ordinal values (0, 1, 2, etc.) to match your SP's column order!
        private void SetupMockReaderForLocalizationData(Mock<IMySqlDataReaderWrapper> mockReader, NotificationTemplateLocalization localization)
        {
            int localizationIdOrdinal = 0;
            int templateIdOrdinal = 1; // Assuming this is the FK TemplateId in the localization table
            int languageCodeOrdinal = 2;
            int localizedSubjectOrdinal = 3; // Crucial for IsDBNull
            int localizedBodyOrdinal = 4;
            int createdAtOrdinal = 5;
            int updatedAtOrdinal = 6;

            mockReader.Setup(r => r.GetOrdinal("LocalizationId")).Returns(localizationIdOrdinal);
            mockReader.Setup(r => r.GetOrdinal("TemplateId")).Returns(templateIdOrdinal);
            mockReader.Setup(r => r.GetOrdinal("LanguageCode")).Returns(languageCodeOrdinal);
            mockReader.Setup(r => r.GetOrdinal("LocalizedSubject")).Returns(localizedSubjectOrdinal);
            mockReader.Setup(r => r.GetOrdinal("LocalizedBody")).Returns(localizedBodyOrdinal);
            mockReader.Setup(r => r.GetOrdinal("CreatedAt")).Returns(createdAtOrdinal);
            mockReader.Setup(r => r.GetOrdinal("UpdatedAt")).Returns(updatedAtOrdinal);

            mockReader.Setup(r => r.GetInt32("LocalizationId")).Returns(localization.LocalizationId);
            mockReader.Setup(r => r.GetInt32("TemplateId")).Returns(localization.TemplateId);
            mockReader.Setup(r => r.GetString("LanguageCode")).Returns(localization.LanguageCode);

            mockReader.Setup(r => r.IsDBNull(localizedSubjectOrdinal)).Returns(localization.LocalizedSubject == null); // Corrected: Use ordinal
            if (localization.LocalizedSubject != null)
            {
                mockReader.Setup(r => r.GetString("LocalizedSubject")).Returns(localization.LocalizedSubject);
            }
            else
            {
                mockReader.Setup(r => r.GetString("LocalizedSubject")).Returns(default(string)); // Return default if SUT calls GetString on DBNull
            }
            mockReader.Setup(r => r.GetString("LocalizedBody")).Returns(localization.LocalizedBody);
            mockReader.Setup(r => r.GetDateTime("CreatedAt")).Returns(localization.CreatedAt);
            mockReader.Setup(r => r.GetDateTime("UpdatedAt")).Returns(localization.UpdatedAt);
        }


        #region InsertNotificationTemplateAsync Tests
        [TestMethod]
        public async Task InsertNotificationTemplateAsync_CallsSPAndReturnsNewId()
        {
            // Arrange
            var template = CreateTestTemplate(id: 0); // ID is auto-generated
            var expectedNewId = 123;
            // SUT parses this string to int, which is fine.
            var outputParams = new Dictionary<string, object> { { "NewTemplateId", expectedNewId.ToString() } };

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
                    It.Is<MySqlParameter[]>(p => p.Length == 1 && (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId")))
                .ReturnsAsync(1); // Simulate 1 row affected

            // Act
            await _service.DeleteNotificationTemplateAsync(templateId);

            // Assert
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync(
                "NotificationTemplateDelete",
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId")), Times.Once);
        }
        #endregion

        #region GetNotificationTemplateByIdAsync Tests
        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_TemplateFound_ReturnsTemplate()
        {
            // Arrange
            var templateId = 1;
            var templateData = CreateTestTemplate(id: templateId);
            var mockDataReader = new Mock<IMySqlDataReaderWrapper>();

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet",
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId")))
                .ReturnsAsync(mockDataReader.Object);

            mockDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplateData(mockDataReader, templateData);

            // Act
            var result = await _service.GetNotificationTemplateByIdAsync(templateId, null); // Pass null for languageCode

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(templateData.TemplateId, result.TemplateId);
            Assert.AreEqual(templateData.TemplateName, result.TemplateName);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_TemplateNotFound_ReturnsNull()
        {
            // Arrange
            var templateId = 404;
            var mockDataReader = new Mock<IMySqlDataReaderWrapper>();

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet",
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId")))
                .ReturnsAsync(mockDataReader.Object);

            mockDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false); // Simulate not found

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
            var defaultTemplateData = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            var localizedTemplateData = CreateTestLocalization(templateId: templateId, languageCode: "es-ES", localizedSubject: "Spanish Subject", localizedBody: "Spanish Body");
            var languageToRequest = "es-ES";

            var mockDefaultDataReader = new Mock<IMySqlDataReaderWrapper>();
            var mockLocalizedDataReader = new Mock<IMySqlDataReaderWrapper>();

            // Setup for the FIRST call (default template)
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && p[0].ParameterName == "@p_TemplateId" && (int)p[0].Value == templateId)))
                .ReturnsAsync(mockDefaultDataReader.Object)
                .Verifiable();

            mockDefaultDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplateData(mockDefaultDataReader, defaultTemplateData);

            // Setup for the SECOND call (localized template)
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 2 &&
                                             p[0].ParameterName == "@p_TemplateId" && (int)p[0].Value == templateId &&
                                             p[1].ParameterName == "@p_LanguageCode" && (string)p[1].Value == languageToRequest)))
                .ReturnsAsync(mockLocalizedDataReader.Object)
                .Verifiable();

            mockLocalizedDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForLocalizationData(mockLocalizedDataReader, localizedTemplateData);

            // Act
            var result = await _service.GetNotificationTemplateByIdAsync(templateId, languageToRequest);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(localizedTemplateData.LocalizedSubject, result.Subject);
            Assert.AreEqual(localizedTemplateData.LocalizedBody, result.Body);
            Assert.AreEqual(defaultTemplateData.TemplateName, result.TemplateName); // Other fields come from default
            _mockMySqlConnectorWrapper.Verify(); // Verifies all verifiable setups
        }


        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_SpecificLanguageDoesNotExist_ReturnsDefaultTemplate()
        {
            var templateId = 1;
            var defaultTemplateData = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            var languageToRequest = "fr-FR"; // Non-existent localization

            var mockDefaultDataReader = new Mock<IMySqlDataReaderWrapper>();
            var mockLocalizationDataReader = new Mock<IMySqlDataReaderWrapper>(); // For the localization call

            // Mock for NotificationTemplateGet (default template)
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId")))
                .ReturnsAsync(mockDefaultDataReader.Object);
            mockDefaultDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplateData(mockDefaultDataReader, defaultTemplateData);

            // Mock for NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode - not found
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", CommandType.StoredProcedure,
                 It.Is<MySqlParameter[]>(p => p.Length == 2 &&
                                              (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId" &&
                                              (string)p[1].Value == languageToRequest && p[1].ParameterName == "@p_LanguageCode")))
                .ReturnsAsync(mockLocalizationDataReader.Object);
            mockLocalizationDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false); // Simulate not found

            var result = await _service.GetNotificationTemplateByIdAsync(templateId, languageToRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplateData.Subject, result.Subject);
            Assert.AreEqual(defaultTemplateData.Body, result.Body);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("NotificationTemplateGet", It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Once);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Once);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_LanguageCodeIsNull_UsesDefaultLanguageAndReturnsDefaultTemplate()
        {
            var templateId = 1;
            var defaultTemplateData = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            var mockDataReader = new Mock<IMySqlDataReaderWrapper>();

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId")))
                .ReturnsAsync(mockDataReader.Object);
            mockDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplateData(mockDataReader, defaultTemplateData);

            var result = await _service.GetNotificationTemplateByIdAsync(templateId, null);

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplateData.Subject, result.Subject);
            Assert.AreEqual(defaultTemplateData.Body, result.Body);
            // Verify localization SP was NOT called because languageCode is null (and default is used)
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync(
               "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Never);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_LanguageCodeIsDefault_ReturnsDefaultTemplateAndSkipsLocalizationCall()
        {
            var templateId = 1;
            var defaultTemplateData = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            var defaultLanguage = "en-US";
            _mockLocalizationSettingsConfig.Setup(ap => ap.Value).Returns(new LocalizationSettingsConfig { DefaultLanguageCode = defaultLanguage });
            var mockDataReader = new Mock<IMySqlDataReaderWrapper>();


            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId")))
                .ReturnsAsync(mockDataReader.Object);
            mockDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplateData(mockDataReader, defaultTemplateData);

            var result = await _service.GetNotificationTemplateByIdAsync(templateId, defaultLanguage); // Requesting default language

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplateData.Subject, result.Subject);
            Assert.AreEqual(defaultTemplateData.Body, result.Body);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Never);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByIdAsync_LocalizedSubjectIsNull_UsesDefaultSubject()
        {
            var templateId = 1;
            var defaultTemplateData = CreateTestTemplate(id: templateId, subject: "Default Subject", body: "Default Body");
            // Localized data has null subject
            var localizedData = CreateTestLocalization(templateId: templateId, languageCode: "es-ES", localizedSubject: null, localizedBody: "Spanish Body");
            var languageToRequest = "es-ES";

            var mockDefaultDataReader = new Mock<IMySqlDataReaderWrapper>();
            var mockLocalizationDataReader = new Mock<IMySqlDataReaderWrapper>();

            // Mock for NotificationTemplateGet (default template)
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGet", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId")))
                .ReturnsAsync(mockDefaultDataReader.Object);
            mockDefaultDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplateData(mockDefaultDataReader, defaultTemplateData);

            // Mock for NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode (localization found)
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 2 &&
                                             (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId" &&
                                             (string)p[1].Value == languageToRequest && p[1].ParameterName == "@p_LanguageCode")))
                .ReturnsAsync(mockLocalizationDataReader.Object);
            mockLocalizationDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true); // Localization found
            SetupMockReaderForLocalizationData(mockLocalizationDataReader, localizedData); // Setup reader for localization (with null subject)

            var result = await _service.GetNotificationTemplateByIdAsync(templateId, languageToRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplateData.Subject, result.Subject); // Fallback to default subject because localized was null
            Assert.AreEqual(localizedData.LocalizedBody, result.Body); // Body should be localized
        }

        #endregion

        #region GetNotificationTemplateByNameAsync Tests
        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_TemplateFound_ReturnsTemplate_DefaultLang()
        {
            // Arrange
            var templateName = "Test Template";
            var templateData = CreateTestTemplate(name: templateName);
            var mockDataReader = new Mock<IMySqlDataReaderWrapper>();

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGetByName",
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (string)p[0].Value == templateName && p[0].ParameterName == "@p_TemplateName")))
                .ReturnsAsync(mockDataReader.Object);

            mockDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplateData(mockDataReader, templateData);

            // Act
            var result = await _service.GetNotificationTemplateByNameAsync(templateName, null); // Default language

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(templateData.TemplateName, result.TemplateName);
            Assert.AreEqual(templateData.Subject, result.Subject); // Should be default subject
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync(
               "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", It.IsAny<CommandType>(), It.IsAny<MySqlParameter[]>()), Times.Never);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_TemplateNotFound_ReturnsNull()
        {
            // Arrange
            var templateName = "NonExistent";
            var mockDataReader = new Mock<IMySqlDataReaderWrapper>();

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGetByName",
                CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (string)p[0].Value == templateName && p[0].ParameterName == "@p_TemplateName")))
                .ReturnsAsync(mockDataReader.Object);

            mockDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            var result = await _service.GetNotificationTemplateByNameAsync(templateName, null);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_SpecificLanguageExists_ReturnsLocalizedTemplate()
        {
            var templateName = "TestName";
            var templateId = 5; // ID from the default template found by name
            var defaultTemplateData = CreateTestTemplate(id: templateId, name: templateName, subject: "Default Subject", body: "Default Body");
            var localizedData = CreateTestLocalization(templateId: templateId, languageCode: "es-ES", localizedSubject: "Spanish Subject", localizedBody: "Spanish Body");
            var languageToRequest = "es-ES";

            var mockDefaultDataReader = new Mock<IMySqlDataReaderWrapper>();
            var mockLocalizationDataReader = new Mock<IMySqlDataReaderWrapper>();

            // Mock for NotificationTemplateGetByName
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGetByName", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (string)p[0].Value == templateName && p[0].ParameterName == "@p_TemplateName")))
                .ReturnsAsync(mockDefaultDataReader.Object);
            mockDefaultDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true); // Default template found
            SetupMockReaderForTemplateData(mockDefaultDataReader, defaultTemplateData); // Setup for default template

            // Mock for NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 2 &&
                                             (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId" &&
                                             (string)p[1].Value == languageToRequest && p[1].ParameterName == "@p_LanguageCode")))
                .ReturnsAsync(mockLocalizationDataReader.Object);
            mockLocalizationDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true); // Localization found
            SetupMockReaderForLocalizationData(mockLocalizationDataReader, localizedData);


            var result = await _service.GetNotificationTemplateByNameAsync(templateName, languageToRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual(localizedData.LocalizedSubject, result.Subject);
            Assert.AreEqual(localizedData.LocalizedBody, result.Body);
            Assert.AreEqual(defaultTemplateData.TemplateName, result.TemplateName);
        }


        [TestMethod]
        public async Task GetNotificationTemplateByNameAsync_SpecificLanguageDoesNotExist_ReturnsDefaultTemplate()
        {
            var templateName = "TestName";
            var templateId = 6;
            var defaultTemplateData = CreateTestTemplate(id: templateId, name: templateName, subject: "Default Subject", body: "Default Body");
            var languageToRequest = "fr-FR";

            var mockDefaultDataReader = new Mock<IMySqlDataReaderWrapper>();
            var mockLocalizationDataReader = new Mock<IMySqlDataReaderWrapper>();


            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateGetByName", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 1 && (string)p[0].Value == templateName && p[0].ParameterName == "@p_TemplateName")))
                .ReturnsAsync(mockDefaultDataReader.Object);
            mockDefaultDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForTemplateData(mockDefaultDataReader, defaultTemplateData);


            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync(
                "NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", CommandType.StoredProcedure,
                It.Is<MySqlParameter[]>(p => p.Length == 2 &&
                                             (int)p[0].Value == templateId && p[0].ParameterName == "@p_TemplateId" &&
                                             (string)p[1].Value == languageToRequest && p[1].ParameterName == "@p_LanguageCode")))
                .ReturnsAsync(mockLocalizationDataReader.Object);
            mockLocalizationDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false); // Localization not found

            var result = await _service.GetNotificationTemplateByNameAsync(templateName, languageToRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual(defaultTemplateData.Subject, result.Subject);
            Assert.AreEqual(defaultTemplateData.Body, result.Body);
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
            var mockDataReader = new Mock<IMySqlDataReaderWrapper>();

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("NotificationTemplateGetAll", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(mockDataReader.Object);

            // Setup sequence for ReadAsync and configure data for each read
            var readCallCount = 0;
            mockDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < templatesData.Count) // Returns true while there's data
                           .Callback(() =>
                           {
                               if (readCallCount < templatesData.Count)
                               {
                                   // Set up the reader for the current template in the list
                                   SetupMockReaderForTemplateData(mockDataReader, templatesData[readCallCount]);
                               }
                               readCallCount++;
                           });

            // Act
            var result = await _service.GetAllNotificationTemplatesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(templatesData.Count, result.Count);
            for (int i = 0; i < templatesData.Count; i++)
            {
                Assert.AreEqual(templatesData[i].TemplateName, result[i].TemplateName);
                Assert.AreEqual(templatesData[i].Subject, result[i].Subject);
            }
        }

        [TestMethod]
        public async Task GetAllNotificationTemplatesAsync_NoTemplatesFound_ReturnsEmptyList()
        {
            var mockDataReader = new Mock<IMySqlDataReaderWrapper>();
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("NotificationTemplateGetAll", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(mockDataReader.Object);
            mockDataReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false); // No data

            var result = await _service.GetAllNotificationTemplatesAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
        #endregion
    }
}
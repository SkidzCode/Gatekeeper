using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Data;
using MySqlConnector;
using System.Linq;

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class SettingsServiceTests
    {
        private Mock<IDbHelper> _mockDbHelper;
        private Mock<IMySqlConnectorWrapper> _mockMySqlConnectorWrapper;
        private Mock<IMySqlDataReaderWrapper> _mockDataReader;
        private Mock<ILogger<SettingsService>> _mockLogger;
        private SettingsService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockDbHelper = new Mock<IDbHelper>();
            _mockMySqlConnectorWrapper = new Mock<IMySqlConnectorWrapper>();
            _mockDataReader = new Mock<IMySqlDataReaderWrapper>();
            _mockLogger = new Mock<ILogger<SettingsService>>();

            _mockDbHelper.Setup(db => db.GetWrapperAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));
            _mockMySqlConnectorWrapper.Setup(c => c.OpenConnectionAsync()).Returns(Task.FromResult(_mockMySqlConnectorWrapper.Object));
            
            _service = new SettingsService(_mockDbHelper.Object, _mockLogger.Object);
        }

        private Setting CreateTestSetting(int id = 1, string name = "TestSetting", string value = "TestValue", 
                                          string category = "General", int? parentId = null, int? userId = null,
                                          string type = "string", string defaultValue = "Default",
                                          int createdBy = 1, int updatedBy = 1)
        {
            return new Setting
            {
                Id = id,
                ParentId = parentId,
                UserId = userId,
                Name = name,
                Category = category,
                SettingValueType = type,
                DefaultSettingValue = defaultValue,
                SettingValue = value,
                CreatedBy = createdBy,
                UpdatedBy = updatedBy,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            };
        }

        private void SetupMockReaderForSetting(Setting setting)
        {
            _mockDataReader.Setup(r => r.GetInt32("Id")).Returns(setting.Id);
            _mockDataReader.Setup(r => r.GetOrdinal("ParentId")).Returns(1); // Example ordinal
            _mockDataReader.Setup(r => r.IsDBNull(1)).Returns(!setting.ParentId.HasValue);
            if (setting.ParentId.HasValue) _mockDataReader.Setup(r => r.GetInt32("ParentId")).Returns(setting.ParentId.Value);
            
            _mockDataReader.Setup(r => r.GetOrdinal("UserId")).Returns(2); // Example ordinal
            _mockDataReader.Setup(r => r.IsDBNull(2)).Returns(!setting.UserId.HasValue);
            if (setting.UserId.HasValue) _mockDataReader.Setup(r => r.GetInt32("UserId")).Returns(setting.UserId.Value);

            _mockDataReader.Setup(r => r.GetString("Name")).Returns(setting.Name);
            _mockDataReader.Setup(r => r.GetOrdinal("Category")).Returns(3); // Example ordinal
            _mockDataReader.Setup(r => r.IsDBNull(3)).Returns(setting.Category == null);
            if (setting.Category != null) _mockDataReader.Setup(r => r.GetString("Category")).Returns(setting.Category);
            
            _mockDataReader.Setup(r => r.GetString("SettingValueType")).Returns(setting.SettingValueType);
            _mockDataReader.Setup(r => r.GetString("DefaultSettingValue")).Returns(setting.DefaultSettingValue);
            _mockDataReader.Setup(r => r.GetString("SettingValue")).Returns(setting.SettingValue);
            _mockDataReader.Setup(r => r.GetInt32("CreatedBy")).Returns(setting.CreatedBy);
            _mockDataReader.Setup(r => r.GetInt32("UpdatedBy")).Returns(setting.UpdatedBy);
            _mockDataReader.Setup(r => r.GetDateTime("CreatedAt")).Returns(setting.CreatedAt);
            _mockDataReader.Setup(r => r.GetDateTime("UpdatedAt")).Returns(setting.UpdatedAt);
        }

        #region GetAllSettingsAsync Tests
        [TestMethod]
        public async Task GetAllSettingsAsync_ReturnsListOfSettings()
        {
            var settingsData = new List<Setting> { CreateTestSetting(1), CreateTestSetting(2) };
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("GetAllSettings", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            var readCallCount = 0;
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < settingsData.Count)
                           .Callback(() => { if (readCallCount < settingsData.Count) SetupMockReaderForSetting(settingsData[readCallCount]); readCallCount++; });
            
            var result = await _service.GetAllSettingsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(settingsData.Count, result.Count);
        }
        #endregion

        #region GetSettingByIdAsync Tests
        [TestMethod]
        public async Task GetSettingByIdAsync_SettingFound_ReturnsSetting()
        {
            var setting = CreateTestSetting(1);
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("GetSettingById", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (int)p[0].Value == setting.Id)))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForSetting(setting);

            var result = await _service.GetSettingByIdAsync(setting.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(setting.Id, result.Id);
        }

        [TestMethod]
        public async Task GetSettingByIdAsync_SettingNotFound_ReturnsNull()
        {
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("GetSettingById", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false);

            var result = await _service.GetSettingByIdAsync(1);
            Assert.IsNull(result);
        }
        #endregion

        #region AddSettingAsync Tests
        [TestMethod]
        public async Task AddSettingAsync_CallsSPAndReturnsSettingWithId()
        {
            var setting = CreateTestSetting(id: 0); // ID is 0 for new setting
            var expectedNewId = 123;
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("AddSetting", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            _mockDataReader.Setup(r => r.GetInt32("NewSettingId")).Returns(expectedNewId);

            var result = await _service.AddSettingAsync(setting);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedNewId, result.Id);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("AddSetting", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }
        #endregion

        #region UpdateSettingAsync Tests
        [TestMethod]
        public async Task UpdateSettingAsync_CallsSPAndReturnsUpdatedSetting()
        {
            var setting = CreateTestSetting(1); // Existing setting
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync("UpdateSetting", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(1); // 1 row affected

            // Mock the GetSettingByIdAsync call that happens after update
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("GetSettingById", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (int)p[0].Value == setting.Id)))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForSetting(setting); // Assume GetSettingById returns the same setting data

            var result = await _service.UpdateSettingAsync(setting);

            Assert.IsNotNull(result);
            Assert.AreEqual(setting.Id, result.Id);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteNonQueryAsync("UpdateSetting", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("GetSettingById", CommandType.StoredProcedure, It.Is<MySqlParameter[]>(p => (int)p[0].Value == setting.Id)), Times.Once);
        }

         [TestMethod]
        public async Task UpdateSettingAsync_UpdateFailed_ReturnsNull()
        {
            var setting = CreateTestSetting(1);
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync("UpdateSetting", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(0); // 0 rows affected

            var result = await _service.UpdateSettingAsync(setting);
            Assert.IsNull(result);
        }
        #endregion

        #region DeleteSettingAsync Tests
        [TestMethod]
        public async Task DeleteSettingAsync_Successful_ReturnsTrue()
        {
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync("DeleteSetting", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(1); // 1 row affected
            var result = await _service.DeleteSettingAsync(1);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task DeleteSettingAsync_Failed_ReturnsFalse()
        {
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteNonQueryAsync("DeleteSetting", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(0); // 0 rows affected
            var result = await _service.DeleteSettingAsync(1);
            Assert.IsFalse(result);
        }
        #endregion

        #region GetSettingsByCategoryAsync Tests
        [TestMethod]
        public async Task GetSettingsByCategoryAsync_ReturnsCorrectSettings()
        {
            var userId = 1;
            var category = "TestCategory";
            var settingsData = new List<Setting> { CreateTestSetting(category: category), CreateTestSetting(id:2, category: category) };
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("GetSettingsByCategory", CommandType.StoredProcedure, 
                It.Is<MySqlParameter[]>(p => (int)p[0].Value == userId && (string)p[1].Value == category)))
                .ReturnsAsync(_mockDataReader.Object);
            var readCallCount = 0;
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < settingsData.Count)
                           .Callback(() => { if (readCallCount < settingsData.Count) SetupMockReaderForSetting(settingsData[readCallCount]); readCallCount++; });

            var result = await _service.GetSettingsByCategoryAsync(userId, category);
            Assert.IsNotNull(result);
            Assert.AreEqual(settingsData.Count, result.Count);
        }
        #endregion

        #region SearchSettingsAsync Tests
        [TestMethod]
        public async Task SearchSettingsAsync_ReturnsMatchingSettings()
        {
            var settingsData = new List<Setting> { CreateTestSetting(name: "SearchMe") };
             _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("SearchSettings", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                .ReturnsAsync(_mockDataReader.Object);
            var readCallCount = 0;
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
                           .ReturnsAsync(() => readCallCount < settingsData.Count)
                           .Callback(() => { if (readCallCount < settingsData.Count) SetupMockReaderForSetting(settingsData[readCallCount]); readCallCount++; });

            var result = await _service.SearchSettingsAsync("SearchMe", null, 10, 0);
            Assert.IsNotNull(result);
            Assert.AreEqual(settingsData.Count, result.Count);
        }
        #endregion
        
        #region AddOrUpdateSettingAsync Tests
        [TestMethod]
        public async Task AddOrUpdateSettingAsync_CallsSPAndReturnsSetting()
        {
            var userId = 1;
            var setting = CreateTestSetting(id: 1, name: "AddOrUpdateTest");
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("AddOrUpdateSetting", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForSetting(setting); // SP returns the setting

            var result = await _service.AddOrUpdateSettingAsync(userId, setting);

            Assert.IsNotNull(result);
            Assert.AreEqual(setting.Name, result.Name);
            _mockMySqlConnectorWrapper.Verify(c => c.ExecuteReaderAsync("AddOrUpdateSetting", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()), Times.Once);
        }

        [TestMethod]
        public async Task AddOrUpdateSettingAsync_NewSettingWithIdZero_CallsSPAndReturnsSetting()
        {
            var userId = 1;
            var setting = CreateTestSetting(id: 0, name: "AddNewViaUpdate"); // ID 0 indicates new
            var returnedSetting = CreateTestSetting(id: 555, name: "AddNewViaUpdate"); // SP returns this with new ID

            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("AddOrUpdateSetting", CommandType.StoredProcedure, 
                It.Is<MySqlParameter[]>(p => p.Any(x => x.ParameterName == "@p_Id" && x.Value == DBNull.Value)))) // Check that Id is passed as DBNull
                .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(true);
            SetupMockReaderForSetting(returnedSetting);

            var result = await _service.AddOrUpdateSettingAsync(userId, setting);

            Assert.IsNotNull(result);
            Assert.AreEqual(returnedSetting.Id, result.Id);
            Assert.AreEqual(setting.Name, result.Name);
        }

        [TestMethod]
        public async Task AddOrUpdateSettingAsync_SPReturnsNoRow_ReturnsNull()
        {
            var userId = 1;
            var setting = CreateTestSetting(id: 1);
            _mockMySqlConnectorWrapper.Setup(c => c.ExecuteReaderAsync("AddOrUpdateSetting", CommandType.StoredProcedure, It.IsAny<MySqlParameter[]>()))
                                      .ReturnsAsync(_mockDataReader.Object);
            _mockDataReader.Setup(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(false); // Simulate no row returned

            var result = await _service.AddOrUpdateSettingAsync(userId, setting);
            Assert.IsNull(result);
        }
        #endregion
    }
}

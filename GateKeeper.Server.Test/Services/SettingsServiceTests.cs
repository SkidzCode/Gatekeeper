using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq; // Keep if used for LINQ expressions in parameter matching

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class SettingsServiceTests
    {
        private Mock<ISettingsRepository> _mockSettingsRepository;
        private Mock<ILogger<SettingsService>> _mockLogger;
        private SettingsService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockSettingsRepository = new Mock<ISettingsRepository>();
            _mockLogger = new Mock<ILogger<SettingsService>>();
            _service = new SettingsService(_mockSettingsRepository.Object, _mockLogger.Object);
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
                UserId = userId, // UserId is part of the Setting model, so keep it for creating test objects
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

        #region GetAllSettingsAsync Tests
        [TestMethod]
        public async Task GetAllSettingsAsync_ReturnsListOfSettingsFromRepository()
        {
            var settingsData = new List<Setting> { CreateTestSetting(1), CreateTestSetting(2, userId: 10) };
            int? testUserId = 10;
            _mockSettingsRepository.Setup(repo => repo.GetAllSettingsAsync(testUserId))
                                   .ReturnsAsync(settingsData.Where(s => s.UserId == testUserId).ToList());

            var result = await _service.GetAllSettingsAsync(testUserId);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count); // Only one setting matches userId 10
            _mockSettingsRepository.Verify(repo => repo.GetAllSettingsAsync(testUserId), Times.Once);
        }

        [TestMethod]
        public async Task GetAllSettingsAsync_NullUserId_ReturnsAllSettingsFromRepository()
        {
            var settingsData = new List<Setting> { CreateTestSetting(1), CreateTestSetting(2) };
            _mockSettingsRepository.Setup(repo => repo.GetAllSettingsAsync(null))
                                   .ReturnsAsync(settingsData);

            var result = await _service.GetAllSettingsAsync(null);

            Assert.IsNotNull(result);
            Assert.AreEqual(settingsData.Count, result.Count);
            _mockSettingsRepository.Verify(repo => repo.GetAllSettingsAsync(null), Times.Once);
        }
        #endregion

        #region GetSettingByIdAsync Tests
        [TestMethod]
        public async Task GetSettingByIdAsync_SettingFound_ReturnsSettingFromRepository()
        {
            var setting = CreateTestSetting(1);
            _mockSettingsRepository.Setup(repo => repo.GetSettingByIdAsync(setting.Id))
                                   .ReturnsAsync(setting);

            var result = await _service.GetSettingByIdAsync(setting.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(setting.Id, result.Id);
            _mockSettingsRepository.Verify(repo => repo.GetSettingByIdAsync(setting.Id), Times.Once);
        }

        [TestMethod]
        public async Task GetSettingByIdAsync_SettingNotFound_ReturnsNullFromRepository()
        {
            _mockSettingsRepository.Setup(repo => repo.GetSettingByIdAsync(It.IsAny<int>()))
                                   .ReturnsAsync((Setting?)null);

            var result = await _service.GetSettingByIdAsync(1);
            Assert.IsNull(result);
            _mockSettingsRepository.Verify(repo => repo.GetSettingByIdAsync(1), Times.Once);
        }
        #endregion

        #region AddSettingAsync Tests
        [TestMethod]
        public async Task AddSettingAsync_CallsRepositoryAndReturnsSetting()
        {
            var settingToAdd = CreateTestSetting(id: 0); // ID is 0 for new setting
            var expectedSettingAfterAdd = CreateTestSetting(id: 123, name: settingToAdd.Name); // Repository returns it with ID

            _mockSettingsRepository.Setup(repo => repo.AddSettingAsync(settingToAdd))
                                   .ReturnsAsync(expectedSettingAfterAdd);

            var result = await _service.AddSettingAsync(settingToAdd);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedSettingAfterAdd.Id, result.Id);
            Assert.AreEqual(settingToAdd.Name, result.Name);
            _mockSettingsRepository.Verify(repo => repo.AddSettingAsync(settingToAdd), Times.Once);
        }
        #endregion

        #region UpdateSettingAsync Tests
        [TestMethod]
        public async Task UpdateSettingAsync_CallsRepositoryAndReturnsUpdatedSetting()
        {
            var settingToUpdate = CreateTestSetting(1, name: "OriginalName");
            var expectedUpdatedSetting = CreateTestSetting(1, name: "UpdatedName");

            _mockSettingsRepository.Setup(repo => repo.UpdateSettingAsync(settingToUpdate))
                                   .ReturnsAsync(expectedUpdatedSetting);

            var result = await _service.UpdateSettingAsync(settingToUpdate);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUpdatedSetting.Id, result.Id);
            Assert.AreEqual(expectedUpdatedSetting.Name, result.Name);
            _mockSettingsRepository.Verify(repo => repo.UpdateSettingAsync(settingToUpdate), Times.Once);
        }

        [TestMethod]
        public async Task UpdateSettingAsync_UpdateFailedInRepository_ReturnsNull()
        {
            var settingToUpdate = CreateTestSetting(1);
            _mockSettingsRepository.Setup(repo => repo.UpdateSettingAsync(settingToUpdate))
                                   .ReturnsAsync((Setting?)null); // Simulate repository failing to update

            var result = await _service.UpdateSettingAsync(settingToUpdate);
            Assert.IsNull(result);
            _mockSettingsRepository.Verify(repo => repo.UpdateSettingAsync(settingToUpdate), Times.Once);
        }
        #endregion

        #region DeleteSettingAsync Tests
        [TestMethod]
        public async Task DeleteSettingAsync_Successful_ReturnsTrueFromRepository()
        {
            _mockSettingsRepository.Setup(repo => repo.DeleteSettingAsync(1)).ReturnsAsync(true);
            var result = await _service.DeleteSettingAsync(1);
            Assert.IsTrue(result);
            _mockSettingsRepository.Verify(repo => repo.DeleteSettingAsync(1), Times.Once);
        }

        [TestMethod]
        public async Task DeleteSettingAsync_Failed_ReturnsFalseFromRepository()
        {
            _mockSettingsRepository.Setup(repo => repo.DeleteSettingAsync(1)).ReturnsAsync(false);
            var result = await _service.DeleteSettingAsync(1);
            Assert.IsFalse(result);
            _mockSettingsRepository.Verify(repo => repo.DeleteSettingAsync(1), Times.Once);
        }
        #endregion

        #region GetSettingsByCategoryAsync Tests
        [TestMethod]
        public async Task GetSettingsByCategoryAsync_ReturnsCorrectSettingsFromRepository()
        {
            var userId = 1;
            var category = "TestCategory";
            var settingsData = new List<Setting> { CreateTestSetting(category: category, userId: userId), CreateTestSetting(id: 2, category: category, userId: userId) };
            _mockSettingsRepository.Setup(repo => repo.GetSettingsByCategoryAsync(userId, category))
                                   .ReturnsAsync(settingsData);

            var result = await _service.GetSettingsByCategoryAsync(userId, category);
            Assert.IsNotNull(result);
            Assert.AreEqual(settingsData.Count, result.Count);
            _mockSettingsRepository.Verify(repo => repo.GetSettingsByCategoryAsync(userId, category), Times.Once);
        }
        #endregion

        #region SearchSettingsAsync Tests
        [TestMethod]
        public async Task SearchSettingsAsync_ReturnsMatchingSettingsFromRepository()
        {
            var name = "SearchMe";
            int limit = 10, offset = 0;
            var settingsData = new List<Setting> { CreateTestSetting(name: name) };
            _mockSettingsRepository.Setup(repo => repo.SearchSettingsAsync(name, null, limit, offset))
                                   .ReturnsAsync(settingsData);

            var result = await _service.SearchSettingsAsync(name, null, limit, offset);
            Assert.IsNotNull(result);
            Assert.AreEqual(settingsData.Count, result.Count);
            _mockSettingsRepository.Verify(repo => repo.SearchSettingsAsync(name, null, limit, offset), Times.Once);
        }
        #endregion

        #region AddOrUpdateSettingAsync Tests
        [TestMethod]
        public async Task AddOrUpdateSettingAsync_CallsRepositoryAndReturnsSetting()
        {
            var userId = 1;
            var settingToUpsert = CreateTestSetting(id: 1, name: "AddOrUpdateTest");
            var expectedReturnedSetting = CreateTestSetting(id: 1, name: "AddOrUpdateTest"); // Assume SP returns the setting

            _mockSettingsRepository.Setup(repo => repo.AddOrUpdateSettingAsync(userId, settingToUpsert))
                                   .ReturnsAsync(expectedReturnedSetting);

            var result = await _service.AddOrUpdateSettingAsync(userId, settingToUpsert);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedReturnedSetting.Name, result.Name);
            _mockSettingsRepository.Verify(repo => repo.AddOrUpdateSettingAsync(userId, settingToUpsert), Times.Once);
        }

        [TestMethod]
        public async Task AddOrUpdateSettingAsync_NewSettingWithIdZero_CallsRepositoryAndReturnsSetting()
        {
            var userId = 1;
            var settingToUpsert = CreateTestSetting(id: 0, name: "AddNewViaUpdate"); // ID 0 indicates new
            var expectedReturnedSetting = CreateTestSetting(id: 555, name: "AddNewViaUpdate"); // Repository returns this with new ID

            _mockSettingsRepository.Setup(repo => repo.AddOrUpdateSettingAsync(userId, settingToUpsert))
                                   .ReturnsAsync(expectedReturnedSetting);

            var result = await _service.AddOrUpdateSettingAsync(userId, settingToUpsert);

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedReturnedSetting.Id, result.Id);
            Assert.AreEqual(settingToUpsert.Name, result.Name);
            _mockSettingsRepository.Verify(repo => repo.AddOrUpdateSettingAsync(userId, settingToUpsert), Times.Once);
        }

        [TestMethod]
        public async Task AddOrUpdateSettingAsync_RepositoryReturnsNull_ReturnsNull()
        {
            var userId = 1;
            var settingToUpsert = CreateTestSetting(id: 1);
            _mockSettingsRepository.Setup(repo => repo.AddOrUpdateSettingAsync(userId, settingToUpsert))
                                   .ReturnsAsync((Setting?)null); // Simulate repository returning null

            var result = await _service.AddOrUpdateSettingAsync(userId, settingToUpsert);
            Assert.IsNull(result);
            _mockSettingsRepository.Verify(repo => repo.AddOrUpdateSettingAsync(userId, settingToUpsert), Times.Once);
        }
        #endregion
    }
}

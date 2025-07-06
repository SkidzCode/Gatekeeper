using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using GateKeeper.Server.Extension;

namespace GateKeeper.Server.Services.Site
{
    public class SettingsService(ISettingsRepository settingsRepository, ILogger<SettingsService> logger) : ISettingsService
    {
        private readonly ISettingsRepository _settingsRepository = settingsRepository;
        private readonly ILogger<SettingsService> _logger = logger;

        /// <summary>
        /// Retrieves all settings.
        /// </summary>
        /// <param name="userId">Optional user Id to filter settings.</param>
        /// <returns>List of Setting objects.</returns>
        public async Task<List<Setting>> GetAllSettingsAsync(int? userId = null)
        {
            try
            {
                _logger.LogInformation("Getting all settings for UserId: {UserId}", userId);
                return await _settingsRepository.GetAllSettingsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllSettingsAsync for UserId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a specific setting by its id.
        /// </summary>
        /// <param name="id">Unique ID of the setting.</param>
        /// <returns>Setting object or null if not found.</returns>
        public async Task<Setting?> GetSettingByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting setting by Id: {SettingId}", id);
                return await _settingsRepository.GetSettingByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSettingByIdAsync with Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Inserts a new setting.
        /// </summary>
        /// <param name="setting">Setting object containing necessary fields.</param>
        /// <returns>The inserted Setting with the new id.</returns>
        public async Task<Setting> AddSettingAsync(Setting setting)
        {
            try
            {
                _logger.LogInformation("Adding new setting: {SettingName}", setting.Name.SanitizeForLogging());
                var addedSetting = await _settingsRepository.AddSettingAsync(setting);
                _logger.LogInformation("Added new setting with Id: {SettingId}", addedSetting.Id);
                return addedSetting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddSettingAsync for SettingName: {SettingName}", setting.Name.SanitizeForLogging());
                throw;
            }
        }

        /// <summary>
        /// Updates an existing setting.
        /// </summary>
        /// <param name="setting">Setting object containing updated fields.</param>
        /// <returns>The updated Setting or null if update failed.</returns>
        public async Task<Setting?> UpdateSettingAsync(Setting setting)
        {
            try
            {
                _logger.LogInformation("Updating setting with Id: {SettingId}", setting.Id);
                var updatedSetting = await _settingsRepository.UpdateSettingAsync(setting);
                if (updatedSetting != null)
                {
                    _logger.LogInformation("Successfully updated setting with Id: {SettingId}", setting.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to update setting with Id: {SettingId}", setting.Id);
                }
                return updatedSetting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateSettingAsync for Id={Id}", setting.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a setting.
        /// </summary>
        /// <param name="id">Unique ID of the setting to delete.</param>
        /// <returns>True if deletion was successful; otherwise, false.</returns>
        public async Task<bool> DeleteSettingAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting setting with Id: {SettingId}", id);
                var result = await _settingsRepository.DeleteSettingAsync(id);
                if (result)
                {
                    _logger.LogInformation("Successfully deleted setting with Id: {SettingId}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to delete setting with Id: {SettingId}", id);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteSettingAsync for Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Retrieves settings by category.
        /// </summary>
        /// <param name="userId">User Id to filter settings.</param>
        /// <param name="category">Category of the settings to retrieve.</param>
        /// <returns>List of Setting objects within the specified category.</returns>
        public async Task<List<Setting>> GetSettingsByCategoryAsync(int userId, string category)
        {
            try
            {
                _logger.LogInformation("Getting settings for UserId: {UserId} and Category: {Category}", userId, category.SanitizeForLogging());
                return await _settingsRepository.GetSettingsByCategoryAsync(userId, category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSettingsByCategoryAsync for UserId: {UserId}, Category='{Category}'", userId, category.SanitizeForLogging());
                throw;
            }
        }

        /// <summary>
        /// Searches settings based on Name and/or Category with pagination.
        /// </summary>
        /// <param name="name">Partial or full name to search for.</param>
        /// <param name="category">Category to filter by.</param>
        /// <param name="limit">Number of records to retrieve.</param>
        /// <param name="offset">Number of records to skip.</param>
        /// <returns>List of matching Setting objects.</returns>
        public async Task<List<Setting>> SearchSettingsAsync(string? name, string? category, int limit, int offset)
        {
            try
            {
                _logger.LogInformation("Searching settings with Name: {Name}, Category: {Category}, Limit: {Limit}, Offset: {Offset}",
                    name.SanitizeForLogging(), category.SanitizeForLogging(), limit, offset);
                return await _settingsRepository.SearchSettingsAsync(name, category, limit, offset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchSettingsAsync with Name='{Name}', Category='{Category}'", name.SanitizeForLogging(), category.SanitizeForLogging());
                throw;
            }
        }

        /// <summary>
        /// Adds or updates a setting.
        /// </summary>
        /// <param name="userId">User id associated with this operation.</param>
        /// <param name="setting">Setting object containing necessary fields.</param>
        /// <returns>The inserted or updated Setting object, or null if operation failed.</returns>
        public async Task<Setting?> AddOrUpdateSettingAsync(int userId, Setting setting)
        {
            try
            {
                _logger.LogInformation("Adding or updating setting: {SettingName} for UserId: {UserId}", setting.Name.SanitizeForLogging(), userId);
                var resultSetting = await _settingsRepository.AddOrUpdateSettingAsync(userId, setting);
                if (resultSetting != null)
                {
                    _logger.LogInformation("Successfully added/updated setting with Id: {SettingId}", resultSetting.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to add or update setting: {SettingName}", setting.Name.SanitizeForLogging());
                }
                return resultSetting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddOrUpdateSettingAsync for SettingName='{SettingName}', UserId: {UserId}", setting.Name.SanitizeForLogging(), userId);
                throw;
            }
        }
    }
}

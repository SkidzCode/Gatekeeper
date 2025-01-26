using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace GateKeeper.Server.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger<SettingsService> _logger;

        /// <summary>
        /// Constructor for the SettingsService.
        /// </summary>
        /// <param name="dbHelper">Database helper for obtaining connections.</param>
        /// <param name="logger">Logger for SettingsService.</param>
        public SettingsService(IDbHelper dbHelper, ILogger<SettingsService> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all settings via the GetAllSettings stored procedure.
        /// </summary>
        /// <param name="userId">Users Id</param>
        /// <returns>List of Setting objects.</returns>
        public async Task<List<Setting>> GetAllSettingsAsync(int? userId = null)
        {
            var settings = new List<Setting>();
            try
            {
                await using var connection = await _dbHelper.GetWrapperAsync();
                await using var reader = await connection.ExecuteReaderAsync("GetAllSettings", CommandType.StoredProcedure,
                    new MySqlParameter("@p_UserId", MySqlDbType.Int32) { Value = userId ?? (object)DBNull.Value });

                while (await reader.ReadAsync())
                {
                    settings.Add(MapReaderToSetting(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllSettingsAsync");
                throw;
            }

            return settings;
        }

        /// <summary>
        /// Retrieves a specific setting by its Id via the GetSettingById stored procedure.
        /// </summary>
        /// <param name="id">Unique ID of the setting.</param>
        /// <returns>Setting object or null if not found.</returns>
        public async Task<Setting?> GetSettingByIdAsync(int id)
        {
            Setting? setting = null;

            try
            {
                await using var connection = await _dbHelper.GetWrapperAsync();
                await using var reader = await connection.ExecuteReaderAsync("GetSettingById", CommandType.StoredProcedure,
                    new MySqlParameter("@p_Id", MySqlDbType.Int32) { Value = id });

                if (await reader.ReadAsync())
                {
                    setting = MapReaderToSetting(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSettingByIdAsync with Id={Id}", id);
                throw;
            }

            return setting;
        }

        /// <summary>
        /// Inserts a new setting via the AddSetting stored procedure.
        /// </summary>
        /// <param name="setting">Setting object containing necessary fields.</param>
        /// <returns>The inserted Setting with the new Id.</returns>
        public async Task<Setting> AddSettingAsync(Setting setting)
        {
            try
            {
                await using var connection = await _dbHelper.GetWrapperAsync();
                await using var reader = await connection.ExecuteReaderAsync("AddSetting", CommandType.StoredProcedure,
                    new MySqlParameter("@p_ParentId", MySqlDbType.Int32) { Value = (object?)setting.ParentId ?? DBNull.Value },
                    new MySqlParameter("@p_Name", MySqlDbType.VarChar, 100) { Value = setting.Name },
                    new MySqlParameter("@p_Category", MySqlDbType.VarChar, 50) { Value = (object?)setting.Category ?? DBNull.Value },
                    new MySqlParameter("@p_UserId", MySqlDbType.Int32) { Value = (object?)setting.UserId ?? DBNull.Value },
                    new MySqlParameter("@p_SettingValueType", MySqlDbType.Enum) { Value = setting.SettingValueType },
                    new MySqlParameter("@p_DefaultSettingValue", MySqlDbType.Text) { Value = setting.DefaultSettingValue },
                    new MySqlParameter("@p_SettingValue", MySqlDbType.Text) { Value = setting.SettingValue },
                    new MySqlParameter("@p_CreatedBy", MySqlDbType.Int32) { Value = setting.CreatedBy },
                    new MySqlParameter("@p_UpdatedBy", MySqlDbType.Int32) { Value = setting.UpdatedBy });

                if (await reader.ReadAsync())
                {
                    setting.Id = reader.GetInt32("NewSettingId");
                }

                return setting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddSettingAsync");
                throw;
            }
        }



        /// <summary>
        /// Updates an existing setting via the UpdateSetting stored procedure.
        /// </summary>
        /// <param name="setting">Setting object containing updated fields.</param>
        /// <returns>The updated Setting or null if update failed.</returns>
        public async Task<Setting?> UpdateSettingAsync(Setting setting)
        {
            try
            {
                await using var connection = await _dbHelper.GetWrapperAsync();
                int rowsAffected = await connection.ExecuteNonQueryAsync("UpdateSetting", CommandType.StoredProcedure,
                    new MySqlParameter("@p_Id", MySqlDbType.Int32) { Value = setting.Id },
                    new MySqlParameter("@p_ParentId", MySqlDbType.Int32) { Value = (object?)setting.ParentId ?? DBNull.Value },
                    new MySqlParameter("@p_Name", MySqlDbType.VarChar, 100) { Value = setting.Name },
                    new MySqlParameter("@p_Category", MySqlDbType.VarChar, 50) { Value = (object?)setting.Category ?? DBNull.Value },
                    new MySqlParameter("@p_SettingValueType", MySqlDbType.Enum) { Value = setting.SettingValueType },
                    new MySqlParameter("@p_DefaultSettingValue", MySqlDbType.Text) { Value = setting.DefaultSettingValue },
                    new MySqlParameter("@p_SettingValue", MySqlDbType.Text) { Value = setting.SettingValue },
                    new MySqlParameter("@p_UpdatedBy", MySqlDbType.Int32) { Value = setting.UpdatedBy });

                if (rowsAffected > 0)
                {
                    return await GetSettingByIdAsync(setting.Id);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateSettingAsync for Id={Id}", setting.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a setting via the DeleteSetting stored procedure.
        /// </summary>
        /// <param name="id">Unique ID of the setting to delete.</param>
        /// <returns>True if deletion was successful; otherwise, false.</returns>
        public async Task<bool> DeleteSettingAsync(int id)
        {
            try
            {
                await using var connection = await _dbHelper.GetWrapperAsync();
                int rowsAffected = await connection.ExecuteNonQueryAsync("DeleteSetting", CommandType.StoredProcedure,
                    new MySqlParameter("@p_Id", MySqlDbType.Int32) { Value = id });

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteSettingAsync for Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Retrieves settings by category via the GetSettingsByCategory stored procedure.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="category">Category of the settings to retrieve.</param>
        /// <returns>List of Setting objects within the specified category.</returns>
        public async Task<List<Setting>> GetSettingsByCategoryAsync(int userId, string category)
        {
            var settings = new List<Setting>();

            try
            {
                await using var connection = await _dbHelper.GetWrapperAsync();
                await using var reader = await connection.ExecuteReaderAsync("GetSettingsByCategory", CommandType.StoredProcedure,
                    new MySqlParameter("@p_UserId", MySqlDbType.Int32) { Value = userId },
                    new MySqlParameter("@p_Category", MySqlDbType.VarChar, 50) { Value = category });

                while (await reader.ReadAsync())
                {
                    settings.Add(MapReaderToSetting(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSettingsByCategoryAsync for Category='{Category}'", category);
                throw;
            }

            return settings;
        }

        /// <summary>
        /// Searches settings based on Name and/or Category with pagination via the SearchSettings stored procedure.
        /// </summary>
        /// <param name="name">Partial or full name to search for.</param>
        /// <param name="category">Category to filter by.</param>
        /// <param name="limit">Number of records to retrieve.</param>
        /// <param name="offset">Number of records to skip.</param>
        /// <returns>List of matching Setting objects.</returns>
        public async Task<List<Setting>> SearchSettingsAsync(string? name, string? category, int limit, int offset)
        {
            var settings = new List<Setting>();

            try
            {
                await using var connection = await _dbHelper.GetWrapperAsync();
                await using var reader = await connection.ExecuteReaderAsync("SearchSettings", CommandType.StoredProcedure,
                    new MySqlParameter("@p_Name", MySqlDbType.VarChar, 100) { Value = (object?)name ?? DBNull.Value },
                    new MySqlParameter("@p_Category", MySqlDbType.VarChar, 50) { Value = (object?)category ?? DBNull.Value },
                    new MySqlParameter("@p_Limit", MySqlDbType.Int32) { Value = limit },
                    new MySqlParameter("@p_Offset", MySqlDbType.Int32) { Value = offset });

                while (await reader.ReadAsync())
                {
                    settings.Add(MapReaderToSetting(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchSettingsAsync with Name='{Name}', Category='{Category}'", name, category);
                throw;
            }

            return settings;
        }

        /// <summary>
        /// Adds or updates a setting via the AddOrUpdateSetting stored procedure.
        /// </summary>
        /// <param name="setting">Setting object containing necessary fields.</param>
        /// <returns>The inserted or updated Setting object, or null if operation failed.</returns>
        public async Task<Setting?> AddOrUpdateSettingAsync(int userId, Setting setting)
        {
            Setting? resultSetting = null;

            try
            {
                int? settingId = setting.Id;
                if (setting.Id == 0)
                    settingId = null;

                await using var connection = await _dbHelper.GetWrapperAsync();
                await using var reader = await connection.ExecuteReaderAsync("AddOrUpdateSetting", CommandType.StoredProcedure,
                    new MySqlParameter("@p_Id", MySqlDbType.Int32) { Value = (object?)settingId ?? DBNull.Value },
                    new MySqlParameter("@p_ParentId", MySqlDbType.Int32) { Value = (object?)setting.ParentId ?? DBNull.Value },
                    new MySqlParameter("@p_Name", MySqlDbType.VarChar, 100) { Value = setting.Name },
                    new MySqlParameter("@p_Category", MySqlDbType.VarChar, 50) { Value = (object?)setting.Category ?? DBNull.Value },
                    new MySqlParameter("@p_UserId", MySqlDbType.Int32) { Value = userId },
                    new MySqlParameter("@p_SettingValueType", MySqlDbType.Enum) { Value = setting.SettingValueType },
                    new MySqlParameter("@p_DefaultSettingValue", MySqlDbType.Text) { Value = setting.DefaultSettingValue },
                    new MySqlParameter("@p_SettingValue", MySqlDbType.Text) { Value = setting.SettingValue },
                    new MySqlParameter("@p_CreatedBy", MySqlDbType.Int32) { Value = setting.CreatedBy },
                    new MySqlParameter("@p_UpdatedBy", MySqlDbType.Int32) { Value = setting.UpdatedBy });

                if (await reader.ReadAsync())
                {
                    resultSetting = MapReaderToSetting(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddOrUpdateSettingAsync for Name='{SettingName}'", setting.Name);
                throw;
            }

            return resultSetting;
        }

        /// <summary>
        /// Helper method to map a data reader row to a Setting object.
        /// </summary>
        /// <param name="reader">Data reader containing the setting data.</param>
        /// <returns>Mapped Setting object.</returns>
        private Setting MapReaderToSetting(IMySqlDataReaderWrapper reader)
        {
            return new Setting
            {
                Id = reader.GetInt32("Id"),
                ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? null : reader.GetInt32("ParentId"),
                Name = reader.GetString("Name"),
                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString("Category"),
                SettingValueType = reader.GetString("SettingValueType"),
                DefaultSettingValue = reader.GetString("DefaultSettingValue"),
                SettingValue = reader.GetString("SettingValue"),
                CreatedBy = reader.GetInt32("CreatedBy"),
                UpdatedBy = reader.GetInt32("UpdatedBy"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                UpdatedAt = reader.GetDateTime("UpdatedAt"),
                UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32("UserId")
            };
        }
    }
}

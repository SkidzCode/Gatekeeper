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
        private readonly IDBHelper _dbHelper;
        private readonly ILogger<SettingsService> _logger;

        /// <summary>
        /// Constructor for the SettingsService.
        /// </summary>
        /// <param name="dbHelper">Database helper for obtaining connections.</param>
        /// <param name="logger">Logger for SettingsService.</param>
        public SettingsService(IDBHelper dbHelper, ILogger<SettingsService> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all settings via the GetAllSettings stored procedure.
        /// </summary>
        /// <returns>List of Setting objects.</returns>
        public async Task<List<Setting>> GetAllSettingsAsync()
        {
            var settings = new List<Setting>();

            try
            {
                await using var connection = await _dbHelper.GetOpenConnectionAsync();
                await using var cmd = new MySqlCommand("GetAllSettings", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                await using var reader = await cmd.ExecuteReaderAsync();
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
        /// <param name="id">Unique Id of the setting.</param>
        /// <returns>Setting object or null if not found.</returns>
        public async Task<Setting?> GetSettingByIdAsync(int id)
        {
            Setting? setting = null;

            try
            {
                await using var connection = await _dbHelper.GetOpenConnectionAsync();
                await using var cmd = new MySqlCommand("GetSettingById", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add(new MySqlParameter("@p_Id", MySqlDbType.Int32)).Value = id;

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    setting = MapReaderToSetting(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetSettingByIdAsync with Id={id}");
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
                await using var connection = await _dbHelper.GetOpenConnectionAsync();
                await using var cmd = new MySqlCommand("AddSetting", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add(new MySqlParameter("@p_ParentId", MySqlDbType.Int32)).Value = (object?)setting.ParentId ?? DBNull.Value;
                cmd.Parameters.Add(new MySqlParameter("@p_Name", MySqlDbType.VarChar, 100)).Value = setting.Name;
                cmd.Parameters.Add(new MySqlParameter("@p_Category", MySqlDbType.VarChar, 50)).Value = (object?)setting.Category ?? DBNull.Value;
                cmd.Parameters.Add(new MySqlParameter("@p_SettingValueType", MySqlDbType.Enum)).Value = setting.SettingValueType;
                cmd.Parameters.Add(new MySqlParameter("@p_DefaultSettingValue", MySqlDbType.Text)).Value = setting.DefaultSettingValue;
                cmd.Parameters.Add(new MySqlParameter("@p_SettingValue", MySqlDbType.Text)).Value = setting.SettingValue;
                cmd.Parameters.Add(new MySqlParameter("@p_CreatedBy", MySqlDbType.Int32)).Value = setting.CreatedBy;
                cmd.Parameters.Add(new MySqlParameter("@p_UpdatedBy", MySqlDbType.Int32)).Value = setting.UpdatedBy;

                // Execute the stored procedure
                await cmd.ExecuteNonQueryAsync();

                // Retrieve the newly inserted Id
                setting.Id = (int)cmd.LastInsertedId;

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
                await using var connection = await _dbHelper.GetOpenConnectionAsync();
                await using var cmd = new MySqlCommand("UpdateSetting", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add(new MySqlParameter("@p_Id", MySqlDbType.Int32)).Value = setting.Id;
                cmd.Parameters.Add(new MySqlParameter("@p_ParentId", MySqlDbType.Int32)).Value = (object?)setting.ParentId ?? DBNull.Value;
                cmd.Parameters.Add(new MySqlParameter("@p_Name", MySqlDbType.VarChar, 100)).Value = setting.Name;
                cmd.Parameters.Add(new MySqlParameter("@p_Category", MySqlDbType.VarChar, 50)).Value = (object?)setting.Category ?? DBNull.Value;
                cmd.Parameters.Add(new MySqlParameter("@p_SettingValueType", MySqlDbType.Enum)).Value = setting.SettingValueType;
                cmd.Parameters.Add(new MySqlParameter("@p_DefaultSettingValue", MySqlDbType.Text)).Value = setting.DefaultSettingValue;
                cmd.Parameters.Add(new MySqlParameter("@p_SettingValue", MySqlDbType.Text)).Value = setting.SettingValue;
                cmd.Parameters.Add(new MySqlParameter("@p_UpdatedBy", MySqlDbType.Int32)).Value = setting.UpdatedBy;

                // Execute the stored procedure
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    // Optionally, retrieve the updated setting
                    return await GetSettingByIdAsync(setting.Id);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in UpdateSettingAsync for Id={setting.Id}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a setting via the DeleteSetting stored procedure.
        /// </summary>
        /// <param name="id">Unique Id of the setting to delete.</param>
        /// <returns>True if deletion was successful; otherwise, false.</returns>
        public async Task<bool> DeleteSettingAsync(int id)
        {
            try
            {
                await using var connection = await _dbHelper.GetOpenConnectionAsync();
                await using var cmd = new MySqlCommand("DeleteSetting", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add(new MySqlParameter("@p_Id", MySqlDbType.Int32)).Value = id;

                // Execute the stored procedure
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DeleteSettingAsync for Id={id}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves settings by category via the GetSettingsByCategory stored procedure.
        /// </summary>
        /// <param name="category">Category of the settings to retrieve.</param>
        /// <returns>List of Setting objects within the specified category.</returns>
        public async Task<List<Setting>> GetSettingsByCategoryAsync(string category)
        {
            var settings = new List<Setting>();

            try
            {
                await using var connection = await _dbHelper.GetOpenConnectionAsync();
                await using var cmd = new MySqlCommand("GetSettingsByCategory", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add(new MySqlParameter("@p_Category", MySqlDbType.VarChar, 50)).Value = category;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    settings.Add(MapReaderToSetting(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetSettingsByCategoryAsync for Category='{category}'");
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
                await using var connection = await _dbHelper.GetOpenConnectionAsync();
                await using var cmd = new MySqlCommand("SearchSettings", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add(new MySqlParameter("@p_Name", MySqlDbType.VarChar, 100)).Value = (object?)name ?? DBNull.Value;
                cmd.Parameters.Add(new MySqlParameter("@p_Category", MySqlDbType.VarChar, 50)).Value = (object?)category ?? DBNull.Value;
                cmd.Parameters.Add(new MySqlParameter("@p_Limit", MySqlDbType.Int32)).Value = limit;
                cmd.Parameters.Add(new MySqlParameter("@p_Offset", MySqlDbType.Int32)).Value = offset;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    settings.Add(MapReaderToSetting(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SearchSettingsAsync with Name='{name}', Category='{category}'");
                throw;
            }

            return settings;
        }

        /// <summary>
        /// Adds or updates a setting via the AddOrUpdateSetting stored procedure.
        /// </summary>
        /// <param name="setting">Setting object containing necessary fields.</param>
        /// <returns>The inserted or updated Setting object, or null if operation failed.</returns>
        public async Task<Setting?> AddOrUpdateSettingAsync(Setting setting)
        {
            Setting? resultSetting = null;

            try
            {
                await using var connection = await _dbHelper.GetOpenConnectionAsync();
                await using var cmd = new MySqlCommand("AddOrUpdateSetting", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add(new MySqlParameter("@p_ParentId", MySqlDbType.Int32)).Value = (object?)setting.ParentId ?? DBNull.Value;
                cmd.Parameters.Add(new MySqlParameter("@p_Name", MySqlDbType.VarChar, 100)).Value = setting.Name;
                cmd.Parameters.Add(new MySqlParameter("@p_Category", MySqlDbType.VarChar, 50)).Value = (object?)setting.Category ?? DBNull.Value;
                cmd.Parameters.Add(new MySqlParameter("@p_SettingValueType", MySqlDbType.Enum)).Value = setting.SettingValueType;
                cmd.Parameters.Add(new MySqlParameter("@p_DefaultSettingValue", MySqlDbType.Text)).Value = setting.DefaultSettingValue;
                cmd.Parameters.Add(new MySqlParameter("@p_SettingValue", MySqlDbType.Text)).Value = setting.SettingValue;
                cmd.Parameters.Add(new MySqlParameter("@p_CreatedBy", MySqlDbType.Int32)).Value = setting.CreatedBy;
                cmd.Parameters.Add(new MySqlParameter("@p_UpdatedBy", MySqlDbType.Int32)).Value = setting.UpdatedBy;

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    resultSetting = MapReaderToSetting(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in AddOrUpdateSettingAsync for Name='{setting.Name}'");
                throw;
            }

            return resultSetting;
        }

        /// <summary>
        /// Helper method to map a data reader row to a Setting object.
        /// </summary>
        /// <param name="reader">Data reader containing the setting data.</param>
        /// <returns>Mapped Setting object.</returns>
        private Setting MapReaderToSetting(MySqlDataReader reader)
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
                UpdatedAt = reader.GetDateTime("UpdatedAt")
            };
        }
    }
}

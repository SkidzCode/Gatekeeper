using Dapper;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GateKeeper.Server.Repositories
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly IDbConnection _dbConnection;

        public SettingsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<List<Setting>> GetAllSettingsAsync(int? userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_UserId", userId, DbType.Int32);
            var settings = await _dbConnection.QueryAsync<Setting>("GetAllSettings", parameters, commandType: CommandType.StoredProcedure);
            return settings.ToList();
        }

        public async Task<Setting?> GetSettingByIdAsync(int id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", id, DbType.Int32);
            return await _dbConnection.QueryFirstOrDefaultAsync<Setting>("GetSettingById", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<Setting> AddSettingAsync(Setting setting)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_ParentId", setting.ParentId, DbType.Int32);
            parameters.Add("@p_Name", setting.Name, DbType.StringFixedLength, size: 100);
            parameters.Add("@p_Category", setting.Category, DbType.StringFixedLength, size: 50);
            parameters.Add("@p_UserId", setting.UserId, DbType.Int32);
            parameters.Add("@p_SettingValueType", setting.SettingValueType, DbType.StringFixedLength, size: 255); // Assuming ENUM is treated as string by Dapper here
            parameters.Add("@p_DefaultSettingValue", setting.DefaultSettingValue, DbType.String);
            parameters.Add("@p_SettingValue", setting.SettingValue, DbType.String);
            parameters.Add("@p_CreatedBy", setting.CreatedBy, DbType.Int32);
            parameters.Add("@p_UpdatedBy", setting.UpdatedBy, DbType.Int32);

            // Stored procedure "AddSetting" is expected to return the new Id
            var newSettingId = await _dbConnection.QuerySingleAsync<int>("AddSetting", parameters, commandType: CommandType.StoredProcedure);
            setting.Id = newSettingId;
            // We might need to fetch the full setting object if AddSetting SP only returns ID and not the full object with CreatedAt/UpdatedAt
            // For now, assuming SP returns enough or we fetch it if needed after this.
            // Based on existing SettingsService, it only sets the ID. We will follow this.
            return setting;
        }

        public async Task<Setting?> UpdateSettingAsync(Setting setting)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", setting.Id, DbType.Int32);
            parameters.Add("@p_ParentId", setting.ParentId, DbType.Int32);
            parameters.Add("@p_Name", setting.Name, DbType.StringFixedLength, size: 100);
            parameters.Add("@p_Category", setting.Category, DbType.StringFixedLength, size: 50);
            // UserId is not updated in the original UpdateSetting SP
            parameters.Add("@p_SettingValueType", setting.SettingValueType, DbType.StringFixedLength, size: 255);
            parameters.Add("@p_DefaultSettingValue", setting.DefaultSettingValue, DbType.String);
            parameters.Add("@p_SettingValue", setting.SettingValue, DbType.String);
            parameters.Add("@p_UpdatedBy", setting.UpdatedBy, DbType.Int32);

            var rowsAffected = await _dbConnection.ExecuteAsync("UpdateSetting", parameters, commandType: CommandType.StoredProcedure);
            if (rowsAffected > 0)
            {
                // The original service refetches the setting. We'll do the same to ensure consistency.
                return await GetSettingByIdAsync(setting.Id);
            }
            return null;
        }

        public async Task<bool> DeleteSettingAsync(int id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", id, DbType.Int32);
            var rowsAffected = await _dbConnection.ExecuteAsync("DeleteSetting", parameters, commandType: CommandType.StoredProcedure);
            return rowsAffected > 0;
        }

        public async Task<List<Setting>> GetSettingsByCategoryAsync(int userId, string category)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_UserId", userId, DbType.Int32);
            parameters.Add("@p_Category", category, DbType.StringFixedLength, size: 50);
            var settings = await _dbConnection.QueryAsync<Setting>("GetSettingsByCategory", parameters, commandType: CommandType.StoredProcedure);
            return settings.ToList();
        }

        public async Task<List<Setting>> SearchSettingsAsync(string? name, string? category, int limit, int offset)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Name", name, DbType.StringFixedLength, size: 100);
            parameters.Add("@p_Category", category, DbType.StringFixedLength, size: 50);
            parameters.Add("@p_Limit", limit, DbType.Int32);
            parameters.Add("@p_Offset", offset, DbType.Int32);
            var settings = await _dbConnection.QueryAsync<Setting>("SearchSettings", parameters, commandType: CommandType.StoredProcedure);
            return settings.ToList();
        }

        public async Task<Setting?> AddOrUpdateSettingAsync(int userId, Setting setting)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_Id", setting.Id == 0 ? null : (int?)setting.Id, DbType.Int32); // Handle 0 as NULL for new setting
            parameters.Add("@p_ParentId", setting.ParentId, DbType.Int32);
            parameters.Add("@p_Name", setting.Name, DbType.StringFixedLength, size: 100);
            parameters.Add("@p_Category", setting.Category, DbType.StringFixedLength, size: 50);
            parameters.Add("@p_UserId", userId, DbType.Int32);
            parameters.Add("@p_SettingValueType", setting.SettingValueType, DbType.StringFixedLength, size: 255);
            parameters.Add("@p_DefaultSettingValue", setting.DefaultSettingValue, DbType.String);
            parameters.Add("@p_SettingValue", setting.SettingValue, DbType.String);
            parameters.Add("@p_CreatedBy", setting.CreatedBy, DbType.Int32);
            parameters.Add("@p_UpdatedBy", setting.UpdatedBy, DbType.Int32);

            // Stored procedure "AddOrUpdateSetting" is expected to return the full setting object
            return await _dbConnection.QueryFirstOrDefaultAsync<Setting>("AddOrUpdateSetting", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}

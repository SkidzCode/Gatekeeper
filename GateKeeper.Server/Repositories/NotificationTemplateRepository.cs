using Dapper;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GateKeeper.Server.Repositories
{
    public class NotificationTemplateRepository : INotificationTemplateRepository
    {
        private readonly IDbConnection _dbConnection;

        public NotificationTemplateRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<int> InsertNotificationTemplateAsync(NotificationTemplate template)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_TemplateName", template.TemplateName, DbType.String);
            parameters.Add("@p_Channel", template.Channel, DbType.String);
            parameters.Add("@p_TokenType", template.TokenType, DbType.String);
            parameters.Add("@p_Subject", template.Subject, DbType.String);
            parameters.Add("@p_Body", template.Body, DbType.String);
            parameters.Add("@p_IsActive", template.IsActive, DbType.Boolean);

            // Assuming NotificationTemplateInsert SP returns the new ID (e.g., via SELECT LAST_INSERT_ID() AS NewTemplateId)
            return await _dbConnection.QuerySingleAsync<int>("NotificationTemplateInsert", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateNotificationTemplateAsync(NotificationTemplate template)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_TemplateId", template.TemplateId, DbType.Int32);
            parameters.Add("@p_TemplateName", template.TemplateName, DbType.String);
            parameters.Add("@p_Channel", template.Channel, DbType.String);
            parameters.Add("@p_TokenType", template.TokenType, DbType.String);
            parameters.Add("@p_Subject", template.Subject, DbType.String);
            parameters.Add("@p_Body", template.Body, DbType.String);
            parameters.Add("@p_IsActive", template.IsActive, DbType.Boolean);

            await _dbConnection.ExecuteAsync("NotificationTemplateUpdate", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteNotificationTemplateAsync(int templateId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_TemplateId", templateId, DbType.Int32);
            await _dbConnection.ExecuteAsync("NotificationTemplateDelete", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<NotificationTemplate?> GetNotificationTemplateByIdAsync(int templateId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_TemplateId", templateId, DbType.Int32);
            return await _dbConnection.QueryFirstOrDefaultAsync<NotificationTemplate>("NotificationTemplateGet", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<NotificationTemplate?> GetNotificationTemplateByNameAsync(string templateName)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_TemplateName", templateName, DbType.String);
            return await _dbConnection.QueryFirstOrDefaultAsync<NotificationTemplate>("NotificationTemplateGetByName", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<List<NotificationTemplate>> GetAllNotificationTemplatesAsync()
        {
            var templates = await _dbConnection.QueryAsync<NotificationTemplate>("NotificationTemplateGetAll", commandType: CommandType.StoredProcedure);
            return templates.ToList();
        }

        public async Task<NotificationTemplateLocalization?> GetLocalizationAsync(int templateId, string languageCode)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@p_TemplateId", templateId, DbType.Int32);
            parameters.Add("@p_LanguageCode", languageCode, DbType.String);

            return await _dbConnection.QueryFirstOrDefaultAsync<NotificationTemplateLocalization>("NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}

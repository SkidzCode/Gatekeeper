using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models;
using GateKeeper.Server.Models.Site;

namespace GateKeeper.Server.Services
{
    public interface INotificationTemplateService
    {
        Task<int> InsertNotificationTemplateAsync(NotificationTemplate template);
        Task UpdateNotificationTemplateAsync(NotificationTemplate template);
        Task DeleteNotificationTemplateAsync(int templateId);
        Task<NotificationTemplate?> GetNotificationTemplateByNameAsync(string templateName);
        Task<NotificationTemplate?> GetNotificationTemplateByIdAsync(int templateId);
        Task<List<NotificationTemplate>> GetAllNotificationTemplatesAsync();
    }

    public class NotificationTemplateService : INotificationTemplateService
    {
        private readonly IDbHelper _dbHelper;

        public NotificationTemplateService(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        /// <summary>
        /// Calls NotificationTemplateInsert and returns the newly inserted Template ID.
        /// </summary>
        public async Task<int> InsertNotificationTemplateAsync(NotificationTemplate template)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@p_TemplateName", template.TemplateName),
                new MySqlParameter("@p_Channel",       template.Channel),
                new MySqlParameter("@p_TokenType",     template.TokenType),
                new MySqlParameter("@p_Subject",       template.Subject),
                new MySqlParameter("@p_Body",          template.Body),
                new MySqlParameter("@p_IsActive",     template.IsActive)
            };

            // We'll use ExecuteNonQueryWithOutputAsync to grab the returned "NewTemplateId"
            var output = await wrapper.ExecuteNonQueryWithOutputAsync(
                commandText: "NotificationTemplateInsert",
                commandType: CommandType.StoredProcedure,
                parameters: parameters
            );

            // The stored procedure returns SELECT LAST_INSERT_ID() AS NewTemplateId
            if (output.TryGetValue("NewTemplateId", out var newTemplateIdObj))
            {
                if (int.TryParse(newTemplateIdObj?.ToString(), out int newId))
                {
                    return newId;
                }
            }

            // Fallback or error handling if needed
            return 0;
        }

        /// <summary>
        /// Calls NotificationTemplateUpdate to update an existing record.
        /// </summary>
        public async Task UpdateNotificationTemplateAsync(NotificationTemplate template)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@p_TemplateId",   template.TemplateId),
                new MySqlParameter("@p_TemplateName", template.TemplateName),
                new MySqlParameter("@p_Channel",       template.Channel),
                new MySqlParameter("@p_TokenType",    template.TokenType),
                new MySqlParameter("@p_Subject",       template.Subject),
                new MySqlParameter("@p_Body",          template.Body),
                new MySqlParameter("@p_IsActive",     template.IsActive)
            };

            await wrapper.ExecuteNonQueryAsync(
                commandText: "NotificationTemplateUpdate",
                commandType: CommandType.StoredProcedure,
                parameters: parameters
            );
        }

        /// <summary>
        /// Calls NotificationTemplateDelete to delete an existing record by ID.
        /// </summary>
        public async Task DeleteNotificationTemplateAsync(int templateId)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@p_TemplateId", templateId)
            };

            await wrapper.ExecuteNonQueryAsync(
                commandText: "NotificationTemplateDelete",
                commandType: CommandType.StoredProcedure,
                parameters: parameters
            );
        }

        /// <summary>
        /// Calls NotificationTemplateGet to fetch a single record by ID.
        /// </summary>
        public async Task<NotificationTemplate?> GetNotificationTemplateByIdAsync(int templateId)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@p_TemplateId", templateId)
            };

            await using var reader = await wrapper.ExecuteReaderAsync(
                commandText: "NotificationTemplateGet",
                commandType: CommandType.StoredProcedure,
                parameters: parameters
            );

            if (await reader.ReadAsync())
            {
                return new NotificationTemplate
                {
                    TemplateId = reader.GetInt32("TemplateId"),
                    TemplateName = reader.GetString("TemplateName"),
                    Channel = reader.GetString("channel"),
                    TokenType = reader.GetString("TokenType"),
                    Subject = reader.GetString("subject"),
                    Body = reader.GetString("body"),
                    IsActive = reader.GetInt32("IsActive") == 1,
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                };
            }

            return null;
        }

        public async Task<NotificationTemplate?> GetNotificationTemplateByNameAsync(string templateName)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@p_TemplateName", templateName)
            };

            await using var reader = await wrapper.ExecuteReaderAsync(
                commandText: "NotificationTemplateGetByName",
                commandType: CommandType.StoredProcedure,
                parameters: parameters
            );

            if (!await reader.ReadAsync()) return null;
            return new NotificationTemplate
            {
                TemplateId = reader.GetInt32("TemplateId"),
                TemplateName = reader.GetString("TemplateName"),
                Channel = reader.GetString("channel"),
                TokenType = reader.GetString("TokenType"),
                Subject = reader.GetString("subject"),
                Body = reader.GetString("body"),
                IsActive = reader.GetInt32("IsActive") == 1,
                CreatedAt = reader.GetDateTime("CreatedAt"),
                UpdatedAt = reader.GetDateTime("UpdatedAt")
            };
        }

        /// <summary>
        /// Calls NotificationTemplateGetAll to fetch all templates.
        /// </summary>
        public async Task<List<NotificationTemplate>> GetAllNotificationTemplatesAsync()
        {
            var results = new List<NotificationTemplate>();

            await using var wrapper = await _dbHelper.GetWrapperAsync();
            await using var reader = await wrapper.ExecuteReaderAsync(
                commandText: "NotificationTemplateGetAll",
                commandType: CommandType.StoredProcedure
            );

            while (await reader.ReadAsync())
            {
                var template = new NotificationTemplate
                {
                    TemplateId = reader.GetInt32("TemplateId"),
                    TemplateName = reader.GetString("TemplateName"),
                    Channel = reader.GetString("channel"),
                    Subject = reader.GetString("subject"),
                    TokenType = reader.GetString("TokenType"),
                    Body = reader.GetString("body"),
                    IsActive = reader.GetInt32("IsActive") == 1,
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                };

                results.Add(template);
            }

            return results;
        }
    }
}

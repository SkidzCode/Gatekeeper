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
        Task<NotificationTemplate?> GetNotificationTemplateByIdAsync(int templateId);
        Task<List<NotificationTemplate>> GetAllNotificationTemplatesAsync();
    }

    public class NotificationTemplateService : INotificationTemplateService
    {
        private readonly IDBHelper _dbHelper;

        public NotificationTemplateService(IDBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        /// <summary>
        /// Calls spInsertNotificationTemplate and returns the newly inserted Template ID.
        /// </summary>
        public async Task<int> InsertNotificationTemplateAsync(NotificationTemplate template)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@p_template_name", template.TemplateName),
                new MySqlParameter("@p_channel",       template.Channel),
                new MySqlParameter("@p_subject",       template.Subject),
                new MySqlParameter("@p_body",          template.Body),
                new MySqlParameter("@p_is_active",     template.IsActive)
            };

            // We'll use ExecuteNonQueryWithOutputAsync to grab the returned "NewTemplateId"
            var output = await wrapper.ExecuteNonQueryWithOutputAsync(
                commandText: "spInsertNotificationTemplate",
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
        /// Calls spUpdateNotificationTemplate to update an existing record.
        /// </summary>
        public async Task UpdateNotificationTemplateAsync(NotificationTemplate template)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@p_template_id",   template.TemplateId),
                new MySqlParameter("@p_template_name", template.TemplateName),
                new MySqlParameter("@p_channel",       template.Channel),
                new MySqlParameter("@p_subject",       template.Subject),
                new MySqlParameter("@p_body",          template.Body),
                new MySqlParameter("@p_is_active",     template.IsActive)
            };

            await wrapper.ExecuteNonQueryAsync(
                commandText: "spUpdateNotificationTemplate",
                commandType: CommandType.StoredProcedure,
                parameters: parameters
            );
        }

        /// <summary>
        /// Calls spDeleteNotificationTemplate to delete an existing record by ID.
        /// </summary>
        public async Task DeleteNotificationTemplateAsync(int templateId)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@p_template_id", templateId)
            };

            await wrapper.ExecuteNonQueryAsync(
                commandText: "spDeleteNotificationTemplate",
                commandType: CommandType.StoredProcedure,
                parameters: parameters
            );
        }

        /// <summary>
        /// Calls spGetNotificationTemplateById to fetch a single record by ID.
        /// </summary>
        public async Task<NotificationTemplate?> GetNotificationTemplateByIdAsync(int templateId)
        {
            await using var wrapper = await _dbHelper.GetWrapperAsync();

            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@p_template_id", templateId)
            };

            await using var reader = await wrapper.ExecuteReaderAsync(
                commandText: "spGetNotificationTemplateById",
                commandType: CommandType.StoredProcedure,
                parameters: parameters
            );

            if (await reader.ReadAsync())
            {
                return new NotificationTemplate
                {
                    TemplateId = reader.GetInt32("template_id"),
                    TemplateName = reader.GetString("template_name"),
                    Channel = reader.GetString("channel"),
                    Subject = reader.GetString("subject"),
                    Body = reader.GetString("body"),
                    IsActive = reader.GetInt32("is_active") == 1,
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                };
            }

            return null;
        }

        /// <summary>
        /// Calls spGetAllNotificationTemplates to fetch all templates.
        /// </summary>
        public async Task<List<NotificationTemplate>> GetAllNotificationTemplatesAsync()
        {
            var results = new List<NotificationTemplate>();

            await using var wrapper = await _dbHelper.GetWrapperAsync();
            await using var reader = await wrapper.ExecuteReaderAsync(
                commandText: "spGetAllNotificationTemplates",
                commandType: CommandType.StoredProcedure
            );

            while (await reader.ReadAsync())
            {
                var template = new NotificationTemplate
                {
                    TemplateId = reader.GetInt32("template_id"),
                    TemplateName = reader.GetString("template_name"),
                    Channel = reader.GetString("channel"),
                    Subject = reader.GetString("subject"),
                    Body = reader.GetString("body"),
                    IsActive = reader.GetInt32("is_active") == 1,
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                };

                results.Add(template);
            }

            return results;
        }
    }
}

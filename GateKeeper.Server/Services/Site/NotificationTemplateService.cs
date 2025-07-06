using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using GateKeeper.Server.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging; // Added for logging
using System;

namespace GateKeeper.Server.Services.Site
{
    public class NotificationTemplateService(
        INotificationTemplateRepository templateRepository,
        IOptions<LocalizationSettingsConfig> localizationSettingsConfig,
        ILogger<NotificationTemplateService> logger)
        : INotificationTemplateService
    {
        private readonly LocalizationSettingsConfig _localizationSettingsConfig = localizationSettingsConfig.Value;

        // Added logger
        // Initialize logger

        // Added logger parameter

        public async Task<int> InsertNotificationTemplateAsync(NotificationTemplate template)
        {
            logger.LogInformation("Inserting notification template: {TemplateName}", template.TemplateName);
            try
            {
                return await templateRepository.InsertNotificationTemplateAsync(template);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inserting notification template: {TemplateName}", template.TemplateName);
                return 0; // Or throw, depending on desired error handling
            }
        }

        public async Task UpdateNotificationTemplateAsync(NotificationTemplate template)
        {
            logger.LogInformation("Updating notification template ID: {TemplateId}", template.TemplateId);
            await templateRepository.UpdateNotificationTemplateAsync(template);
        }

        public async Task DeleteNotificationTemplateAsync(int templateId)
        {
            logger.LogInformation("Deleting notification template ID: {TemplateId}", templateId);
            await templateRepository.DeleteNotificationTemplateAsync(templateId);
        }

        public async Task<NotificationTemplate?> GetNotificationTemplateByIdAsync(int templateId, string? languageCode = null)
        {
            logger.LogDebug("Fetching notification template by ID: {TemplateId}, Language: {LanguageCode}", templateId, languageCode ?? "default");
            var template = await templateRepository.GetNotificationTemplateByIdAsync(templateId);
            if (template == null)
            {
                logger.LogWarning("Notification template not found for ID: {TemplateId}", templateId);
                return null;
            }

            return await ApplyLocalization(template, languageCode);
        }

        public async Task<NotificationTemplate?> GetNotificationTemplateByNameAsync(string templateName, string? languageCode = null)
        {
            logger.LogDebug("Fetching notification template by Name: {TemplateName}, Language: {LanguageCode}", templateName, languageCode ?? "default");
            var template = await templateRepository.GetNotificationTemplateByNameAsync(templateName);
            if (template == null)
            {
                logger.LogWarning("Notification template not found for Name: {TemplateName}", templateName);
                return null;
            }
            return await ApplyLocalization(template, languageCode);
        }

        private async Task<NotificationTemplate?> ApplyLocalization(NotificationTemplate template, string? languageCode)
        {
            string effectiveLanguageCode = string.IsNullOrWhiteSpace(languageCode)
                ? _localizationSettingsConfig.DefaultLanguageCode
                : languageCode;

            logger.LogDebug("Effective language code for template ID {TemplateId}: {EffectiveLanguageCode}", template.TemplateId, effectiveLanguageCode);

            if (string.Equals(effectiveLanguageCode, _localizationSettingsConfig.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Requested language is default. Returning template ID {TemplateId} without further localization lookup.", template.TemplateId);
                return template; // No need to fetch localization if it's the default language
            }

            var localization = await templateRepository.GetLocalizationAsync(template.TemplateId, effectiveLanguageCode);
            if (localization != null)
            {
                logger.LogInformation("Localization found for template ID {TemplateId} in language {EffectiveLanguageCode}. Applying.", template.TemplateId, effectiveLanguageCode);
                template.Subject = string.IsNullOrEmpty(localization.LocalizedSubject) ? template.Subject : localization.LocalizedSubject;
                template.Body = localization.LocalizedBody; // Assuming body should always be replaced if localization exists
            }
            else
            {
                logger.LogDebug("No localization found for template ID {TemplateId} in language {EffectiveLanguageCode}. Returning default content.", template.TemplateId, effectiveLanguageCode);
            }
            return template;
        }

        public async Task<List<NotificationTemplate>> GetAllNotificationTemplatesAsync()
        {
            logger.LogInformation("Fetching all notification templates.");
            return await templateRepository.GetAllNotificationTemplatesAsync();
        }
    }
}

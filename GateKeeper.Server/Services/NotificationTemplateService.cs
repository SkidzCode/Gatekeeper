using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Site;
using GateKeeper.Server.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging; // Added for logging
using System; // For StringComparison

namespace GateKeeper.Server.Services
{
    // Interface INotificationTemplateService is now expected to be in its own file:
    // GateKeeper.Server/Interface/INotificationTemplateService.cs
    // If it's not, you should move it there or adjust the using directive.

    public class NotificationTemplateService : INotificationTemplateService
    {
        private readonly INotificationTemplateRepository _templateRepository;
        private readonly LocalizationSettingsConfig _localizationSettingsConfig;
        private readonly ILogger<NotificationTemplateService> _logger; // Added logger

        public NotificationTemplateService(
            INotificationTemplateRepository templateRepository,
            IOptions<LocalizationSettingsConfig> localizationSettingsConfig,
            ILogger<NotificationTemplateService> logger) // Added logger parameter
        {
            _templateRepository = templateRepository;
            _localizationSettingsConfig = localizationSettingsConfig.Value;
            _logger = logger; // Initialize logger
        }

        public async Task<int> InsertNotificationTemplateAsync(NotificationTemplate template)
        {
            _logger.LogInformation("Inserting notification template: {TemplateName}", template.TemplateName);
            try
            {
                return await _templateRepository.InsertNotificationTemplateAsync(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting notification template: {TemplateName}", template.TemplateName);
                return 0; // Or throw, depending on desired error handling
            }
        }

        public async Task UpdateNotificationTemplateAsync(NotificationTemplate template)
        {
            _logger.LogInformation("Updating notification template ID: {TemplateId}", template.TemplateId);
            await _templateRepository.UpdateNotificationTemplateAsync(template);
        }

        public async Task DeleteNotificationTemplateAsync(int templateId)
        {
            _logger.LogInformation("Deleting notification template ID: {TemplateId}", templateId);
            await _templateRepository.DeleteNotificationTemplateAsync(templateId);
        }

        public async Task<NotificationTemplate?> GetNotificationTemplateByIdAsync(int templateId, string? languageCode = null)
        {
            _logger.LogDebug("Fetching notification template by ID: {TemplateId}, Language: {LanguageCode}", templateId, languageCode ?? "default");
            var template = await _templateRepository.GetNotificationTemplateByIdAsync(templateId);
            if (template == null)
            {
                _logger.LogWarning("Notification template not found for ID: {TemplateId}", templateId);
                return null;
            }

            return await ApplyLocalization(template, languageCode);
        }

        public async Task<NotificationTemplate?> GetNotificationTemplateByNameAsync(string templateName, string? languageCode = null)
        {
            _logger.LogDebug("Fetching notification template by Name: {TemplateName}, Language: {LanguageCode}", templateName, languageCode ?? "default");
            var template = await _templateRepository.GetNotificationTemplateByNameAsync(templateName);
            if (template == null)
            {
                _logger.LogWarning("Notification template not found for Name: {TemplateName}", templateName);
                return null;
            }
            return await ApplyLocalization(template, languageCode);
        }

        private async Task<NotificationTemplate?> ApplyLocalization(NotificationTemplate template, string? languageCode)
        {
            string effectiveLanguageCode = string.IsNullOrWhiteSpace(languageCode)
                ? _localizationSettingsConfig.DefaultLanguageCode
                : languageCode;

            _logger.LogDebug("Effective language code for template ID {TemplateId}: {EffectiveLanguageCode}", template.TemplateId, effectiveLanguageCode);

            if (string.Equals(effectiveLanguageCode, _localizationSettingsConfig.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Requested language is default. Returning template ID {TemplateId} without further localization lookup.", template.TemplateId);
                return template; // No need to fetch localization if it's the default language
            }

            var localization = await _templateRepository.GetLocalizationAsync(template.TemplateId, effectiveLanguageCode);
            if (localization != null)
            {
                _logger.LogInformation("Localization found for template ID {TemplateId} in language {EffectiveLanguageCode}. Applying.", template.TemplateId, effectiveLanguageCode);
                template.Subject = string.IsNullOrEmpty(localization.LocalizedSubject) ? template.Subject : localization.LocalizedSubject;
                template.Body = localization.LocalizedBody; // Assuming body should always be replaced if localization exists
            }
            else
            {
                _logger.LogDebug("No localization found for template ID {TemplateId} in language {EffectiveLanguageCode}. Returning default content.", template.TemplateId, effectiveLanguageCode);
            }
            return template;
        }

        public async Task<List<NotificationTemplate>> GetAllNotificationTemplatesAsync()
        {
            _logger.LogInformation("Fetching all notification templates.");
            return await _templateRepository.GetAllNotificationTemplatesAsync();
        }
    }
}

using GateKeeper.Server.Models.Site;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    public interface INotificationTemplateRepository
    {
        Task<int> InsertNotificationTemplateAsync(NotificationTemplate template);
        Task UpdateNotificationTemplateAsync(NotificationTemplate template);
        Task DeleteNotificationTemplateAsync(int templateId);
        Task<NotificationTemplate?> GetNotificationTemplateByIdAsync(int templateId);
        Task<NotificationTemplate?> GetNotificationTemplateByNameAsync(string templateName);
        Task<List<NotificationTemplate>> GetAllNotificationTemplatesAsync();
        Task<NotificationTemplateLocalization?> GetLocalizationAsync(int templateId, string languageCode);
        // Potentially methods for localization CUD operations if they are separate
        // Task<int> InsertLocalizationAsync(NotificationTemplateLocalization localization);
        // Task UpdateLocalizationAsync(NotificationTemplateLocalization localization);
        // Task DeleteLocalizationAsync(int localizationId);
    }
}

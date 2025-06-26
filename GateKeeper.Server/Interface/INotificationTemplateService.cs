using System.Collections.Generic;
using System.Threading.Tasks;
using GateKeeper.Server.Models.Site;

namespace GateKeeper.Server.Interface
{
    public interface INotificationTemplateService
    {
        Task<int> InsertNotificationTemplateAsync(NotificationTemplate template);
        Task UpdateNotificationTemplateAsync(NotificationTemplate template);
        Task DeleteNotificationTemplateAsync(int templateId);
        Task<NotificationTemplate?> GetNotificationTemplateByIdAsync(int templateId, string? languageCode = null);
        Task<NotificationTemplate?> GetNotificationTemplateByNameAsync(string templateName, string? languageCode = null);
        Task<List<NotificationTemplate>> GetAllNotificationTemplatesAsync();
    }
}

using GateKeeper.Server.Models.Site;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GateKeeper.Server.Interface
{
    public interface ISettingsRepository
    {
        Task<List<Setting>> GetAllSettingsAsync(int? userId);
        Task<Setting?> GetSettingByIdAsync(int id);
        Task<Setting> AddSettingAsync(Setting setting);
        Task<Setting?> UpdateSettingAsync(Setting setting);
        Task<bool> DeleteSettingAsync(int id);
        Task<List<Setting>> GetSettingsByCategoryAsync(int userId, string category);
        Task<List<Setting>> SearchSettingsAsync(string? name, string? category, int limit, int offset);
        Task<Setting?> AddOrUpdateSettingAsync(int userId, Setting setting);
    }
}

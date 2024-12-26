using GateKeeper.Server.Models.Site;

namespace GateKeeper.Server.Interface
{
    public interface ISettingsService
    {
        Task<List<Setting>> GetAllSettingsAsync();
        Task<Setting?> GetSettingByIdAsync(int id);
        Task<Setting> AddSettingAsync(Setting setting);
        Task<Setting?> UpdateSettingAsync(Setting setting);
        Task<bool> DeleteSettingAsync(int id);
        Task<List<Setting>> GetSettingsByCategoryAsync(string category);
        Task<List<Setting>> SearchSettingsAsync(string? name, string? category, int limit, int offset);
        Task<Setting?> AddOrUpdateSettingAsync(Setting setting);
    }
}
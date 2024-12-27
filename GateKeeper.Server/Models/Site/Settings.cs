namespace GateKeeper.Server.Models.Site
{
    public class Setting
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public int? UserId { get; set; } = null;
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string SettingValueType { get; set; } = string.Empty;
        public string DefaultSettingValue { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

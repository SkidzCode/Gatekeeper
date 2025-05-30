namespace GateKeeper.Server.Models.Site
{
    public class NotificationTemplateLocalization
    {
        public int LocalizationId { get; set; }
        public int TemplateId { get; set; }
        public string LanguageCode { get; set; } = default!;
        public string? LocalizedSubject { get; set; } // Nullable as per plan
        public string LocalizedBody { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

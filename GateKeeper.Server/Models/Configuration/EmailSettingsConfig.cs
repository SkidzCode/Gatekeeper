// GateKeeper.Server/Models/Configuration/EmailSettingsConfig.cs
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Configuration
{
    public class EmailSettingsConfig
    {
        public const string SectionName = "EmailSettings";

        [Required]
        public string SmtpServer { get; set; }

        [Range(1, 65535)]
        public int Port { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string FromAddress { get; set; }
    }
}

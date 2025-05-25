// GateKeeper.Server/Models/Configuration/PasswordSettingsConfig.cs
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Configuration
{
    public class PasswordSettingsConfig
    {
        public const string SectionName = "PasswordSettings";

        [Range(1, int.MaxValue)]
        public int RequiredLength { get; set; }

        public bool RequireDigit { get; set; }

        public bool RequireLowercase { get; set; }

        public bool RequireUppercase { get; set; }

        public bool RequireNonAlphanumeric { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxFailedAccessAttempts { get; set; } // For lockout
    }
}

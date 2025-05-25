// GateKeeper.Server/Models/Configuration/LoginSettingsConfig.cs
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Configuration
{
    public class LoginSettingsConfig
    {
        public const string SectionName = "LoginSettings";

        [Range(1, 100)]
        public int MaxFailedAccessAttempts { get; set; }

        [Range(1, int.MaxValue)]
        public int CookieExpiryMinutes { get; set; } // Renamed from LockoutDurationInMinutes to match usage

        public bool LockoutEnabled { get; set; }

        // Adding LockoutDurationInMinutes as it's a common related setting,
        // though not explicitly seen in current code usage.
        // If not in appsettings.json, it will get its default value (0 for int).
        [Range(1, int.MaxValue)]
        public int LockoutDurationInMinutes { get; set; } = 15; // Default if not specified
    }
}

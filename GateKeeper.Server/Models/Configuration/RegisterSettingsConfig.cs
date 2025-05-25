// GateKeeper.Server/Models/Configuration/RegisterSettingsConfig.cs
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Configuration
{
    public class RegisterSettingsConfig
    {
        public const string SectionName = "RegisterSettings";

        // Based on current usage: _configuration.GetValue<bool>("RegisterSettings:RequireInvite");
        public bool RequireInvite { get; set; }

        // Adding DefaultRole as it was in the example and is common,
        // though not explicitly seen in the current IConfiguration direct usage.
        // If not in appsettings.json, it will get its default value (null for string).
        // Add [Required] if it's mandatory.
        public string? DefaultRole { get; set; } // Made nullable if not always present
        public bool RequireEmailConfirmation { get; set; } // From example
    }
}

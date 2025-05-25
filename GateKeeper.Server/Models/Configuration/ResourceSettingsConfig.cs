// GateKeeper.Server/Models/Configuration/ResourceSettingsConfig.cs
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Configuration
{
    public class ResourceSettingsConfig
    {
        public const string SectionName = "Resources"; // Assuming "Resources" is the section name

        [Required]
        public string Path { get; set; }
    }
}

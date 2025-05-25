// GateKeeper.Server/Models/Configuration/JwtSettingsConfig.cs
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Configuration
{
    public class JwtSettingsConfig
    {
        public const string SectionName = "Jwt";

        [Required]
        public string Key { get; set; }

        [Required]
        public string Issuer { get; set; }

        [Required]
        public string Audience { get; set; }

        [Range(1, int.MaxValue)]
        public int TokenValidityInMinutes { get; set; }

        [Range(1, int.MaxValue)]
        public int RefreshTokenValidityInDays { get; set; }
    }
}

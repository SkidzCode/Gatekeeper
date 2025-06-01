// GateKeeper.Server/Models/Configuration/DatabaseConfig.cs
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Configuration
{
    public class DatabaseConfig
    {
        public const string SectionName = "ConnectionStrings"; // Or "ConnectionStrings" if that's the section name in appsettings.json

        [Required]
        public string GateKeeperConnection { get; set; }
    }
}

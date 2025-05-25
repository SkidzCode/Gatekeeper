// GateKeeper.Server/Models/Configuration/DatabaseConfig.cs
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Configuration
{
    public class DatabaseConfig
    {
        public const string SectionName = "DatabaseConfig"; // Or "ConnectionStrings" if that's the section name in appsettings.json

        [Required]
        public string ConnectionString { get; set; } 
    }
}

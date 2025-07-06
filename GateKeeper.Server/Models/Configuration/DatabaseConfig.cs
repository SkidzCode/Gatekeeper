// GateKeeper.Server/Models/Configuration/DatabaseConfig.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Configuration
{
    public class DatabaseConfig
    {
        public const string SectionName = "ConnectionStrings";

        [Required]
        [MinLength(1, ErrorMessage = "At least one connection string must be configured.")]
        public List<ConnectionDetail> Connections { get; set; } = new List<ConnectionDetail>();
    }

    public class ConnectionDetail
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string ConnectionString { get; set; }
    }
}

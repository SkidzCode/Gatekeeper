// GateKeeper.Server/Models/Configuration/KeyManagementConfig.cs
using System.ComponentModel.DataAnnotations;

namespace GateKeeper.Server.Models.Configuration
{
    public class KeyManagementConfig
    {
        public const string SectionName = "KeyManagement";

        [Required]
        // Add any other constraints, e.g., MinLength if appropriate, though for keys, exact length might be enforced by the crypto library.
        public string MasterKey { get; set; }
    }
}

// GateKeeper.Server/Models/Configuration/SerilogConfig.cs
namespace GateKeeper.Server.Models.Configuration
{
    public class SerilogConfig
    {
        public const string SectionName = "Serilog";

        public bool EnableHashing { get; set; }
    }
}

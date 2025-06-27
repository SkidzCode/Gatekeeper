using GateKeeper.Plugin.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace GateKeeper.Plugin.Placeholder
{
    public class PlaceholderPlugin : IPlugin
    {
        public string Name => "Placeholder Plugin";
        public string Version => "0.1.0";
        public string Description => "A non-functional plugin to test loading and API exposure.";
        // Set any other IPlugin properties to null or empty strings if they exist on IPlugin interface now.
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // No services to register for this placeholder
        }
    }
}

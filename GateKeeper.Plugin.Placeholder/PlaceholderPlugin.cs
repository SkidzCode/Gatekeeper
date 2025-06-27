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
        public string AngularModulePath => ""; // Or a specific path if it had one
        public string AngularModuleName => ""; // Or a specific module name
        public string DefaultRoutePath => "";  // Or a specific route
        public string NavigationLabel => "Placeholder"; // Or empty
        public string RequiredRole => null; // Or specific role / empty string

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // No services to register for this placeholder
        }
    }
}

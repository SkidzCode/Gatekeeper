using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace GateKeeper.Plugin.Abstractions
{
    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }
        string Description { get; }
        // Frontend-related properties (can be added now or deferred to Phase 3/4,
        // but good to consider their future existence):
        // string AngularModulePath { get; } // e.g., "plugins/sample/sample.module" (from src/app/)
        // string AngularModuleName { get; } // e.g., "SampleModule"
        // string DefaultRoutePath { get; } // e.g., "sample-plugin"
        // string NavigationLabel { get; } // e.g., "Sample Plugin"
        // string RequiredRole { get; } // Optional: Role to access

        void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    }
}

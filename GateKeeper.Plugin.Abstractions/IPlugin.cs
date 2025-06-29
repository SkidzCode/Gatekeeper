using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace GateKeeper.Plugin.Abstractions
{
    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }
        string Description { get; }
        string AngularModulePath { get; }
        string AngularModuleName { get; }
        string DefaultRoutePath { get; }
        string NavigationLabel { get; }
        string RequiredRole { get; }

        // New properties for Admin section
        string? AdminAngularModulePath { get; }
        string? AdminAngularModuleName { get; }
        string? AdminDefaultRoutePath { get; }
        string? AdminNavigationLabel { get; }
        string? AdminRequiredRole { get; }

        void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    }
}

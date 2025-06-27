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

        void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    }
}

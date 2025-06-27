using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace GateKeeper.Plugin.Abstractions
{
    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }
        string Description { get; }

        void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    }
}

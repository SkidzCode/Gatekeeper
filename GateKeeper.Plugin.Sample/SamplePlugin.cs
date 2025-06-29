using GateKeeper.Plugin.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GateKeeper.Plugin.Sample
{
    public class SamplePlugin : IPlugin
    {
        public string Name => "Sample Plugin";
        public string Version => "1.0.0";
        public string Description => "A sample plugin for GateKeeper.";
        public string AngularModulePath => "sample/sample.module";
        public string AngularModuleName => "SampleModule";
        public string DefaultRoutePath => "sample";
        public string NavigationLabel => "Sample Tools";
        public string RequiredRole => "User";

        // Admin section properties (null for now as no admin section exists)
        public string? AdminAngularModulePath => null;
        public string? AdminAngularModuleName => null;
        public string? AdminDefaultRoutePath => null;
        public string? AdminNavigationLabel => null;
        public string? AdminRequiredRole => null;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // services.AddScoped<ISampleService, SampleService>();
        }
    }
}

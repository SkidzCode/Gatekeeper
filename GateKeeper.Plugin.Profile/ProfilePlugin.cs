using GateKeeper.Plugin.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GateKeeper.Plugin.Profile
{
    public class ProfilePlugin : IPlugin
    {
        public string Name => "User Profile Plugin";
        public string Version => "1.0.0";
        public string Description => "A plugin to display user profile information.";
        public string AngularModulePath => "profile/profile.module"; // Used by esbuild
        public string AngularModuleName => "ProfileModule"; // Name of the Angular module class
        public string DefaultRoutePath => "profile"; // Base path for plugin routes (e.g., portal/profile)
        public string NavigationLabel => "Profile"; // Text for the navigation link
        public string RequiredRole => "User"; // Role required to access this plugin

        // Admin section properties (null for now as no admin section exists)
        public string? AdminAngularModulePath => null;
        public string? AdminAngularModuleName => null;
        public string? AdminDefaultRoutePath => null;
        public string? AdminNavigationLabel => null;
        public string? AdminRequiredRole => null;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register any services specific to this plugin here
            // For example:
            // services.AddScoped<IProfileService, ProfileService>();
        }
    }
}

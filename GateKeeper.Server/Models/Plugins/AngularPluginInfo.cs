namespace GateKeeper.Server.Models.Plugins
{
    public class AngularPluginInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        // Add other properties from IPlugin that will be sent to frontend later
        // public string AngularModulePath { get; set; }
        // public string AngularModuleName { get; set; }
        // public string RoutePath { get; set; } // This will be the full path, e.g., "portal/plugin-slug"
        // public string NavigationLabel { get; set; }
        // public string RequiredRole { get; set; }
    }
}

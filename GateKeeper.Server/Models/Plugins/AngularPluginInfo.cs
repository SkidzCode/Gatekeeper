namespace GateKeeper.Server.Models.Plugins
{
    public class AngularPluginInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string AngularModulePath { get; set; }
        public string AngularModuleName { get; set; }
        public string RoutePath { get; set; }
        public string NavigationLabel { get; set; }
        public string RequiredRole { get; set; }
    }
}

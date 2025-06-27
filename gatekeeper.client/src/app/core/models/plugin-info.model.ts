export interface AngularPluginInfo {
  name: string;
  version: string;
  description: string;
  defaultRoutePath: string; // Renamed from routePath
  angularModulePath: string; // Corresponds to AngularModulePath in backend
  angularModuleName: string; // Corresponds to AngularModuleName in backend
  navigationLabel: string;
  requiredRole?: string; // Optional
}

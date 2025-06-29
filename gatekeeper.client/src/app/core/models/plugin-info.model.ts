export interface AngularPluginInfo {
  name: string;
  version: string;
  description: string;
  routePath: string; // Reverted to original name
  angularModulePath: string; // Corresponds to AngularModulePath in backend
  angularModuleName: string; // Corresponds to AngularModuleName in backend
  navigationLabel: string;
  requiredRole?: string; // Optional

  // New properties for Admin section
  adminAngularModulePath?: string;
  adminAngularModuleName?: string;
  adminRoutePath?: string;
  adminNavigationLabel?: string;
  adminRequiredRole?: string;
}

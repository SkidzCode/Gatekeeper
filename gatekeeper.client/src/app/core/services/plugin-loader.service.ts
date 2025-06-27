import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router, Route } from '@angular/router';
import { AngularPluginInfo } from '../models/plugin-info.model';

@Injectable({
  providedIn: 'root'
})
export class PluginLoaderService {
  private pluginManifests: AngularPluginInfo[] = [];

  constructor(private http: HttpClient, private router: Router) { }

  async loadPluginManifests(): Promise<void> {
    try {
      const manifests = await this.http.get<AngularPluginInfo[]>('/api/plugins/manifests').toPromise();
      this.pluginManifests = manifests || [];
      console.log('Plugin manifests loaded:', this.pluginManifests); // Optional: for verification

      if (this.pluginManifests.length > 0) {
        const pluginRoutes: Route[] = this.pluginManifests
          .map(plugin => {
            if (!plugin.angularModulePath || typeof plugin.angularModulePath !== 'string' || plugin.angularModulePath.trim() === '') {
              console.error(`Invalid or missing angularModulePath for plugin "${plugin.name}". Skipping.`);
              return null;
            }
            if (!plugin.angularModuleName || typeof plugin.angularModuleName !== 'string' || plugin.angularModuleName.trim() === '') {
              console.error(`Invalid or missing angularModuleName for plugin "${plugin.name}". Skipping.`);
              return null;
            }
            if (!plugin.routePath || typeof plugin.routePath !== 'string' || !plugin.routePath.includes('/')) {
                 console.error(`Invalid routePath: "${plugin.routePath}" for plugin "${plugin.name}". It should be at least two segments long (e.g., layout/pluginpath).`);
                 return null;
            }

            const pathSegments = plugin.routePath.split('/');
            const pluginSpecificPath = pathSegments[pathSegments.length - 1];

            // Ensure angularModulePath does not start or end with a slash to prevent path issues like `../../plugins//sample/sample.module` or `../../plugins/sample/sample.module/`
            const cleanAngularModulePath = plugin.angularModulePath.replace(/^\/+|\/+$/g, '');

            return {
              path: pluginSpecificPath,
              // Assuming cleanAngularModulePath from backend is like "plugins/sample/sample.module"
              // and plugin-loader.service.ts is in app/core/services/
              loadChildren: () => import(`../../${cleanAngularModulePath}`).then(m => m[plugin.angularModuleName]),
              data: {
                navigationLabel: plugin.navigationLabel,
                requiredRole: plugin.requiredRole
              }
            };
          })
          .filter(route => route !== null) as Route[];

        if (pluginRoutes.length > 0) {
          const currentConfig = this.router.config;
          let portalRoute = currentConfig.find(r => r.path === 'portal');

          if (portalRoute) {
            if (!portalRoute.children) {
              portalRoute.children = [];
            }

            // Explicitly check portalRoute.children again before assignment and use
            if (portalRoute.children) {
              const childrenRoutes = portalRoute.children; // Safe to assign now
              pluginRoutes.forEach(pluginRoute => {
                if (!childrenRoutes.find(child => child.path === pluginRoute.path)) {
                  childrenRoutes.push(pluginRoute);
                } else {
                  // Use bracket notation for data property access and ensure data object exists
                  const navLabel = pluginRoute.data && typeof pluginRoute.data === 'object' && pluginRoute.data['navigationLabel']
                                   ? pluginRoute.data['navigationLabel']
                                   : 'unknown';
                  console.warn(`Route path conflict: A route with path "${pluginRoute.path}" already exists under "portal". Plugin route for "${navLabel}" will be skipped.`);
                }
              });
              this.router.resetConfig(currentConfig);
            } else {
              // This path should ideally not be reached if the above initialization works
              console.error('Critical Error: portalRoute.children is null or undefined even after initialization attempt.');
            }
            console.log('Router configuration updated with plugin routes:', pluginRoutes);
          } else {
            console.error('Error: "portal" route not found. Cannot add plugin routes.');
          }
        }
      }
    } catch (error) {
      console.error('Error loading plugin manifests or integrating routes:', error);
    }
  }

  getPluginManifests(): AngularPluginInfo[] {
    return this.pluginManifests;
  }
}

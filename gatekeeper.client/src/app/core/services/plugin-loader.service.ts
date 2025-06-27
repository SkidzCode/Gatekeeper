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
            const pathSegments = plugin.routePath.split('/');
            // Expects routePath like "portal/sample" or "admin/another"
            if (pathSegments.length < 2) {
              console.error(`Invalid routePath: "${plugin.routePath}" for plugin "${plugin.name}". It should be at least two segments long (e.g., layout/pluginpath).`);
              return null;
            }
            const pluginSpecificPath = pathSegments[pathSegments.length - 1];

            return {
              path: pluginSpecificPath,
              // Assuming plugin.angularModulePath from backend is like "plugins/sample/sample.module"
              // and plugin-loader.service.ts is in app/core/services/
              loadChildren: () => import(`../../${plugin.angularModulePath}`).then(m => m[plugin.angularModuleName]),
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

            // Add only new routes, avoiding conflicts
            pluginRoutes.forEach(pluginRoute => {
              if (!portalRoute.children.find(child => child.path === pluginRoute.path)) {
                portalRoute.children.push(pluginRoute);
              } else {
                console.warn(`Route path conflict: A route with path "${pluginRoute.path}" already exists under "portal". Plugin route for "${pluginRoute.data?.navigationLabel}" will be skipped.`);
              }
            });

            this.router.resetConfig(currentConfig);
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

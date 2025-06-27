import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AngularPluginInfo } from '../models/plugin-info.model';

@Injectable({
  providedIn: 'root'
})
export class PluginLoaderService {
  private pluginManifests: AngularPluginInfo[] = [];

  constructor(private http: HttpClient) { }

  async loadPluginManifests(): Promise<void> {
    try {
      const manifests = await this.http.get<AngularPluginInfo[]>('/api/plugins/manifests').toPromise();
      this.pluginManifests = manifests || [];
      console.log('Plugin manifests loaded:', this.pluginManifests); // Optional: for verification
    } catch (error) {
      console.error('Error loading plugin manifests:', error);
    }
  }

  getPluginManifests(): AngularPluginInfo[] {
    return this.pluginManifests;
  }
}

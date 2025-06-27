import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AngularPluginInfo } from '../models/plugin-info.model';

@Injectable({
  providedIn: 'root'
})
export class PluginLoaderService {
  private pluginManifests: AngularPluginInfo[] = [];
  public manifestLoadingPromise: Promise<void> | null = null;

  constructor(private http: HttpClient) { }

  loadPluginManifests(): Promise<void> {
    if (this.manifestLoadingPromise) {
      console.log('[PluginLoaderService] loadPluginManifests: Already loading or loaded.');
      return this.manifestLoadingPromise;
    }

    console.log('[PluginLoaderService] loadPluginManifests: Starting to load...');
    this.manifestLoadingPromise = (async () => { // IIFE to use async/await and assign promise
      try {
        const manifests = await this.http.get<AngularPluginInfo[]>('/api/plugins/manifests').toPromise();
        this.pluginManifests = manifests || [];
        console.log('[PluginLoaderService] Plugin manifests loaded successfully:', this.pluginManifests);
      } catch (error) {
        console.error('[PluginLoaderService] Error loading plugin manifests:', error);
        this.pluginManifests = []; // Ensure it's empty on error
        // Optionally re-throw if manifests are critical, so APP_INITIALIZER fails clearly
        // throw error;
      }
    })();
    return this.manifestLoadingPromise;
  }

  getPluginManifests(): AngularPluginInfo[] {
    // This log helps see when getPluginManifests is called relative to loading
    console.log('[PluginLoaderService] getPluginManifests called, manifest count:', this.pluginManifests.length);
    return this.pluginManifests;
  }
}

// src/app/services/settings.service.ts

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Setting } from '../../../shared/models/setting.model';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class SettingsService {
  // Use environment variable for base URL
  private baseUrl = '/api/Settings';

  constructor(private http: HttpClient) { }

  /**
   * Retrieves all settings via GET /api/Settings.
   * @returns An observable containing an array of Setting objects.
   */
  getAllSettings(): Observable<Setting[]> {
    return this.http.get<Setting[]>(this.baseUrl);
  }

  /**
   * Retrieves a specific setting by Id via GET /api/Settings/{id}.
   * @param id The unique identifier of the setting.
   * @returns An observable containing the Setting object.
   */
  getSettingById(id: number): Observable<Setting> {
    const url = `${this.baseUrl}/${id}`;
    return this.http.get<Setting>(url);
  }

  /**
   * Creates a new setting via POST /api/Settings.
   * @param setting A Setting object containing the details of the new setting.
   * @returns An observable containing the server's response with the created setting.
   */
  addSetting(setting: Omit<Setting, 'id' | 'createdAt' | 'updatedAt'>): Observable<{ message: string; setting: Setting }> {
    return this.http.post<{ message: string; setting: Setting }>(this.baseUrl, setting);
  }

  /**
   * Updates an existing setting via PUT /api/Settings/{id}.
   * @param setting The Setting object containing updated information.
   * @returns An observable containing the server's response with the updated setting.
   */
  updateSetting(setting: Setting): Observable<{ message: string; setting: Setting }> {
    const url = `${this.baseUrl}/${setting.id}`;
    return this.http.put<{ message: string; setting: Setting }>(url, setting);
  }

  /**
   * Deletes a setting by its Id via DELETE /api/Settings/{id}.
   * @param id The unique identifier of the setting to delete.
   * @returns An observable containing the server's response message.
   */
  deleteSetting(id: number): Observable<{ message: string }> {
    const url = `${this.baseUrl}/${id}`;
    return this.http.delete<{ message: string }>(url);
  }

  /**
   * Retrieves all settings within a specific category via GET /api/Settings/Category/{category}.
   * @param category The category of settings to retrieve.
   * @returns An observable containing an array of Setting objects within the specified category.
   */
  getSettingsByCategory(category: string): Observable<Setting[]> {
    const url = `${this.baseUrl}/Category/${encodeURIComponent(category)}`;
    return this.http.get<Setting[]>(url);
  }

  /**
   * Searches settings based on name and/or category with pagination via GET /api/Settings/Search.
   * @param name (Optional) Partial or full name to search for.
   * @param category (Optional) Category to filter by.
   * @param limit (Optional, default = 10) Number of records to retrieve.
   * @param offset (Optional, default = 0) Number of records to skip.
   * @returns An observable containing an array of matching Setting objects.
   */
  searchSettings(name?: string, category?: string, limit: number = 10, offset: number = 0): Observable<Setting[]> {
    let params = new HttpParams();
    if (name) {
      params = params.set('name', name);
    }
    if (category) {
      params = params.set('category', category);
    }
    params = params.set('limit', limit.toString());
    params = params.set('offset', offset.toString());

    const url = `${this.baseUrl}/Search`;
    return this.http.get<Setting[]>(url, { params });
  }

  /**
   * Adds a new setting or updates it if it already exists based on the Name via POST /api/Settings/AddOrUpdate.
   * @param setting A Setting object containing the details of the setting to add or update.
   * @returns An observable containing the server's response with the added or updated setting.
   */
  addOrUpdateSetting(setting: Omit<Setting, 'createdAt' | 'updatedAt'>): Observable<{ message: string; setting: Setting }> {
    const url = `${this.baseUrl}/AddOrUpdate`;
    return this.http.post<{ message: string; setting: Setting }>(url, setting);
  }
}

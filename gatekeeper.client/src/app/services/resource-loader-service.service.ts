// src/app/services/resource-loader.service.ts

import { Injectable } from '@angular/core';
import { ResourcesService, ResourceEntry } from './ResourcesService';
import { Observable, of } from 'rxjs';
import { catchError, map, shareReplay, tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class ResourceLoaderService {
  private resourceCache: Map<string, { [key: string]: string }> = new Map();

  constructor(private resourcesService: ResourcesService) { }

  /**
   * Loads the resources for the specified file and caches them.
   * @param resourceFileName The name of the resource file without extension.
   * @returns An Observable of a dictionary-like object for resource key-value pairs.
   */
  loadResourceFile(resourceFileName: string): Observable<{ [key: string]: string }> {
    if (this.resourceCache.has(resourceFileName)) {
      return of(this.resourceCache.get(resourceFileName) as { [key: string]: string });
    }

    return this.resourcesService.getEntries(resourceFileName).pipe(
      map((entries: ResourceEntry[]) => {
        const resourceDict: { [key: string]: string } = {};
        entries.forEach(entry => {
          resourceDict[entry.key] = entry.value;
        });
        return resourceDict;
      }),
      tap(resourceDict => this.resourceCache.set(resourceFileName, resourceDict)),
      shareReplay(1), // Ensures the same Observable is shared if called multiple times
      catchError(error => {
        console.error(`Failed to load resource file: ${resourceFileName}`, error);
        return of({});
      })
    );
  }

  /**
   * Retrieves a specific resource value by key from the cached resources.
   * @param resourceFileName The name of the resource file without extension.
   * @param key The key of the resource entry.
   * @returns The resource value if found, or an empty string otherwise.
   */
  getResourceValue(resourceFileName: string, key: string): string {
    const resourceDict = this.resourceCache.get(resourceFileName);
    return resourceDict?.[key] ?? '';
  }
}

// src/app/services/resources.service.ts

import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

// src/app/shared/models/resource-entry.model.ts

export interface ResourceEntry {
  key: string;
  value: string;
  comment: string;
}

export interface AddResourceEntryRequest {
  key: string;
  value: string;
}

export interface UpdateResourceEntryRequest {
  value: string;
  comment: string;
  type: string;
}



@Injectable({
  providedIn: 'root'
})
export class ResourcesService {
  private baseUrl = '/api/Resources';

  constructor(private http: HttpClient) { }

  /**
   * Retrieves all entries from the specified resource file.
   * @param resourceFileName The name of the resource file without extension.
   * @returns An Observable of an array of ResourceEntry.
   */
  getEntries(resourceFileName: string): Observable<ResourceEntry[]> {
    const url = `${this.baseUrl}/${encodeURIComponent(resourceFileName)}`;
    return this.http.get<ResourceEntry[]>(url).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Adds a new entry to the specified resource file.
   * @param resourceFileName The name of the resource file without extension.
   * @param entry The entry to add.
   * @returns An Observable of the added entry.
   */
  addEntry(resourceFileName: string, entry: AddResourceEntryRequest): Observable<ResourceEntry> {
    const url = `${this.baseUrl}/${encodeURIComponent(resourceFileName)}`;
    return this.http.post<ResourceEntry>(url, entry).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Updates an existing entry in the specified resource file.
   * @param resourceFileName The name of the resource file without extension.
   * @param key The key of the entry to update.
   * @param entry The updated entry data.
   * @returns An Observable of any type (since the API returns NoContent).
   */
  updateEntry(resourceFileName: string, key: string, entry: UpdateResourceEntryRequest): Observable<void> {
    const url = `${this.baseUrl}/${encodeURIComponent(resourceFileName)}/${encodeURIComponent(key)}`;
    return this.http.put<void>(url, entry).pipe(
      catchError(error => {
        console.error(`Error updating entry:`, {
          resourceFileName,
          key,
          entry,
          error
        });
        return throwError(() => new Error(`Failed to update entry: ${error.message || error}`));
      })
    );
  }


  /**
   * Handles HTTP errors and formats them for the caller.
   * @param error The HTTP error response.
   * @returns An Observable that throws a user-friendly error message.
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMsg = 'An unknown error occurred while processing your request.';

    if (error.error instanceof ErrorEvent) {
      // Client-side or network error
      errorMsg = `Client-side error: ${error.error.message}`;
    } else if (error.status === 404) {
      // Resource not found
      errorMsg = 'The requested resource was not found.';
    } else if (error.status === 400) {
      // Bad request
      errorMsg = 'Invalid request. Please check the input data.';
    } else if (error.error?.error) {
      // Server-side error with error message
      errorMsg = error.error.error;
    } else if (error.message) {
      // Other server-side error
      errorMsg = error.message;
    }

    return throwError(() => errorMsg);
  }
}

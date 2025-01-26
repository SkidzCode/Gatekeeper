import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SessionModel } from '../../../shared/models/session.model';

@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private baseUrl = '/api/Services';

  constructor(private http: HttpClient) { }

  /**
   * Retrieves a list of all active sessions for the authenticated user.
   * @returns An observable that emits the list of active sessions.
   */
  getActiveSessions(): Observable<SessionModel[]> {
    return this.http.get<SessionModel[]>(`${this.baseUrl}/sessions/active`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Retrieves the most recent activity sessions.
   * @returns An observable that emits the list of recent activity sessions.
   */
  getMostRecentActivity(): Observable<SessionModel[]> {
    return this.http.get<SessionModel[]>(`${this.baseUrl}/sessions/recent`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Handles HTTP errors from server responses.
   * @param error The error response from the HTTP client.
   * @returns An observable throwing a user-friendly error message.
   */
  private handleError(error: HttpErrorResponse) {
    let errorMsg = 'An unknown error occurred.';
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMsg = `Error: ${error.error.message}`;
    } else if (error.error?.error) {
      // Server-side error with a custom error message
      errorMsg = error.error.error;
    } else if (error.message) {
      // Other server-side error
      errorMsg = error.message;
    }
    return throwError(() => errorMsg);
  }
}

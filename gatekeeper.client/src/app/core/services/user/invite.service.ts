import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, map, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

// Example Invite interface (adjust as needed)
export interface Invite {
  id?: number;
  fromId: number;
  toName: string;
  toEmail: string;
  verificationId?: string;
  notificationId?: number;
  created?: string; // Or Date


  // Fields from the JOIN
  isExpired?: boolean;
  isRevoked?: boolean;
  isComplete?: boolean;
  isSent?: boolean;

  website: string;
}

interface CheckInviteRequiredResponse {
  _requiresInvite: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class InviteService {
  private baseUrl = '/api/Invite';

  constructor(private http: HttpClient) { }

  /**
   * Sends an invite to a user via POST /api/Invite/SendInvite
   * @param invite The invite data to send
   * @returns An observable with { message: string, inviteId: number } upon success
   */
  sendInvite(invite: Invite): Observable<{ message: string; inviteId: number }> {
    invite.website = window.location.origin;
    return this.http.post<{ message: string; inviteId: number }>(
      `${this.baseUrl}/SendInvite`, invite
    )
      .pipe(
        catchError(this.handleError)
      );
  }

  /**
   * Retrieves invites by a given FromId (the sender's user ID).
   * GET /api/Invite/from/{fromId}
   * @param fromId The ID of the user who sent the invites
   * @returns An observable of Invite[]
   */
  getInvitesByFromId(fromId: number): Observable<Invite[]> {
    return this.http.get<Invite[]>(`${this.baseUrl}/from/${fromId}`)
      .pipe(
        catchError(this.handleError)
      );
  }

  checkInviteRequired(): Observable<boolean> {
    return this.http.get<CheckInviteRequiredResponse>(`${this.baseUrl}/is-invite-only`).pipe(
      map(response => {
        console.log('Response from is-invite-only:', response); // Add logging
        return response._requiresInvite;
      }),
      catchError((error) => {
        console.error('Error in checkInviteRequired:', error); // Add logging
        return of(true); // In case of error, assume invite is required
      })
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

    return throwError(() => new Error(errorMsg));
  }
}

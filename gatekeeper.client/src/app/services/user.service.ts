import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, throwError, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { User } from '../models/user.model';
import { Role } from '../models/role.model';
import { UserEdit } from '../models/user.edit.model';


@Injectable({ providedIn: 'root' })
export class UserService {
  private baseUrl = '/api/User';

  constructor(private http: HttpClient) { }

  /**
   * Retrieves the profile of the currently authenticated user.
   * @returns An observable that emits the User profile data.
   */
  getProfile(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/profile`).pipe(
      catchError(this.handleError)
    );
  }

  /**
 * Retrieves the profile of the currently authenticated user.
 * @returns An observable that emits the User profile data.
 */
  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.baseUrl}/users`).pipe(
      catchError(this.handleError)
    );
  }

  getUserById(id: number): Observable<User> {
    // On .NET side, you might have an endpoint like: GET /api/User/users/{id}
    return this.http.get<User>(`${this.baseUrl}/user/${id}`).pipe(
      catchError(this.handleError)
    );
  }

  getUserByIdEdit(id: number): Observable<UserEdit> {
    // On .NET side, you might have an endpoint like: GET /api/User/users/{id}
    return this.http.get<UserEdit>(`${this.baseUrl}/user/edit/${id}`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Updates the authenticated user's details.
   * Please note that the endpoint requires the user data in query parameters.
   * @param user The updated user information.
   * @returns An observable that emits a success message upon successful update.
   */
  updateUser(user: User): Observable<{ message: string }> {
    const payload = {
      id: user.id,
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      username: user.username,
      phone: user.phone,
    };

    return this.http.post<{ message: string }>(`${this.baseUrl}/Update`, payload).pipe(
      tap(() => {
        console.log('Sending POST request to:', `${this.baseUrl}/Update`);
        console.log('Payload:', payload);
      }),
      catchError((error: HttpErrorResponse) => {
        let errorMessage = 'An error occurred';
        if (error.error instanceof ErrorEvent) {
          // Client-side or network error
          errorMessage = `Client-side error: ${error.error.message}`;
        } else {
          // Server-side error
          errorMessage = `Server-side error: Status: ${error.status}, Message: ${error.message}`;
        }

        // Log the error details for debugging
        console.error('Error details:', {
          status: error.status,
          message: error.message,
          error: error.error,
        });

        // You can add more specific error messages based on status codes if needed
        switch (error.status) {
          case 0:
            errorMessage += ' - No connection. Please check the server.';
            break;
          case 404:
            errorMessage += ' - Endpoint not found.';
            break;
          case 500:
            errorMessage += ' - Internal server error.';
            break;
          default:
            errorMessage += ' - Something went wrong.';
        }

        return throwError(() => new Error(errorMessage));
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
    return throwError(() => errorMsg);
  }
}

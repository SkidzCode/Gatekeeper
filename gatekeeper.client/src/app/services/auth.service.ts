import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject, map, of } from 'rxjs';
import { catchError, tap,  } from 'rxjs/operators';

// Existing Interface
interface LoginRequest {
  identifier: string;
  password: string;
}

interface User {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  username: string;
  phone: string;
}

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

interface RefreshRequest {
  refreshToken: string;
}

// New Interface
interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  username: string;
  password: string;
  phone: string;
}

interface VerifyUserRequest {
  verificationCode: string;
}

interface PasswordResetInitiateRequest {
  emailOrUsername: string;
  website: string;
}

interface PasswordResetRequest {
  resetToken: string;
  newPassword: string;
}

interface PasswordChangeRequest {
  currentPassword: string;
  newPassword: string;
}

interface ValidatePasswordRequest {
  password: string;
}

interface LogoutRequest {
  token: string;
}

interface LogoutDeviceRequest {
  sessionId?: string;
}

interface Session {
  sessionId: string;
  device: string;
  ipAddress: string;
  createdAt: string;
  lastActive: string;
}

interface CheckUsernameResponse {
  isValid: boolean;
}

interface CheckEmailResponse {
  isValid: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private baseUrl = '/api/Authentication';

  // Current User Subject (Optional)
  private currentUserSubject = new BehaviorSubject<User | null>(this.getUser());
  public currentUser$ = this.currentUserSubject.asObservable();

  // Access Token Subject
  private currentAccessTokenSubject = new BehaviorSubject<string | null>(this.getAccessToken());
  public currentAccessToken$ = this.currentAccessTokenSubject.asObservable();

  constructor(private http: HttpClient) { }

  // Existing Methods

  login(identifier: string, password: string): Observable<AuthResponse> {
    const body: LoginRequest = { identifier, password };
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, body).pipe(
      tap((res: AuthResponse) => {
        this.setTokens(res.accessToken, res.refreshToken);
        this.setUser(res.user);
      }),
      catchError(this.handleError)
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => 'No refresh token stored.');
    }

    const body: RefreshRequest = { refreshToken };
    return this.http.post<AuthResponse>(`${this.baseUrl}/refresh-token`, body).pipe(
      tap((res: AuthResponse) => {
        this.setTokens(res.accessToken, res.refreshToken);
        this.setUser(res.user);
      }),
      catchError(this.handleError)
    );
  }

  logout(): void {
    // Optionally call the server to invalidate the token
    this.clearTokens();
    this.clearUser();
  }

  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  private setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    this.currentAccessTokenSubject.next(accessToken);
  }

  private clearTokens(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    this.currentAccessTokenSubject.next(null);
  }

  // New Methods

  /**
   * Registers a new user.
   * @param registerData User registration details.
   */
  register(registerData: RegisterRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/register`, registerData).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Checks if a username is already taken.
   * @param username The username to check.
   * @returns Observable<boolean> - true if taken, false otherwise.
   */
  checkUsernameTaken(username: string): Observable<boolean> {
    const params = new HttpParams().set('username', username);
    return this.http.get<CheckUsernameResponse>(`${this.baseUrl}/check-username`, { params }).pipe(
      map(response => !response.isValid), // If isValid is false, username is taken
      catchError(() => of(false)) // In case of error, assume username is not taken
    );
  }

  /**
 * Checks if a username is already taken.
 * @param email The username to check.
 * @returns Observable<boolean> - true if taken, false otherwise.
 */
  checkEmailTaken(email: string): Observable<boolean> {
    const params = new HttpParams().set('email', email);
    return this.http.get<CheckEmailResponse>(`${this.baseUrl}/check-email`, { params }).pipe(
      map(response => !response.isValid), // If isValid is false, username is taken
      catchError(() => of(false)) // In case of error, assume username is not taken
    );
  }

  /**
   * Verifies a new user using a verification code.
   * @param verificationCode The verification code sent to the user.
   */
  verifyUser(verificationCode: string): Observable<{ message: string }> {
    const url = `${this.baseUrl}/verify-user`;
    const body = { verificationCode };

    return this.http.post<{ message: string }>(url, body).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Initiates the password reset process.
   * @param emailOrUsername The user's email or username.
   */
  initiatePasswordReset(emailOrUsername: PasswordResetInitiateRequest): Observable<{ message: string }> {
    
    return this.http.post<{ message: string }>(`${this.baseUrl}/password-reset/initiate`, emailOrUsername).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Resets the user's password using a reset token.
   * @param resetData The reset token and new password.
   */
  resetPassword(resetData: PasswordResetRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/password-reset/reset`, resetData).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Validates the strength of a password.
   * @param password The password to validate.
   */
  validatePasswordStrength(password: string): Observable<{ isValid: boolean }> {
    const body: ValidatePasswordRequest = { password };
    return this.http.post<{ isValid: boolean }>(`${this.baseUrl}/validate-password`, body).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Retrieves active sessions for the authenticated user.
   */
  getActiveSessions(): Observable<Session[]> {
    return this.http.get<Session[]>(`${this.baseUrl}/sessions`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Logs out the user from a specific device or all devices.
   * @param sessionId Optional session ID to logout from a specific device.
   */
  logoutFromDevice(sessionId?: string): Observable<{ message: string }> {
    const body: LogoutDeviceRequest = { sessionId };
    return this.http.post<{ message: string }>(`${this.baseUrl}/logout-device`, body).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Logs out the user from the current session.
   */
  logoutCurrentSession(): Observable<{ message: string }> {
    const body: LogoutRequest = { token: this.getAccessToken() ?? '' };
    return this.http.post<{ message: string }>(`${this.baseUrl}/logout`, body).pipe(
      tap(() => {
        this.clearTokens();
        this.clearUser();
      }),
      catchError(this.handleError)
    );
  }

  // User Management (Optional)
  public setUser(user: User): void {
    localStorage.setItem('currentUser', JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  public getUser(): User | null {
    const userJson = localStorage.getItem('currentUser');
    return userJson ? JSON.parse(userJson) : null;
  }

  private clearUser(): void {
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(null);
  }

  // Error Handling
  private handleError(error: HttpErrorResponse) {
    let errorMsg = 'An unknown error occurred.';
    if (error.error instanceof ErrorEvent) {
      // Client-side/network error
      errorMsg = `Error: ${error.error.message}`;
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

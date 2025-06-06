import { Injectable } from '@angular/core';
import {
  HttpEvent, HttpInterceptor, HttpHandler, HttpRequest, HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take, distinctUntilChanged } from 'rxjs/operators';
import { AuthService } from '../services/user/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private refreshTokenInProgress = false;
  private refreshTokenSubject: BehaviorSubject<string | null> = new BehaviorSubject<string | null>(null);
  private retryCount = 0;
  private readonly maxRetries = 3;

  constructor(private authService: AuthService, private router: Router) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    let authReq = req;
    const accessToken = this.authService.getAccessToken();

    if (accessToken && !this.isExcludedUrl(req.url)) {
      authReq = this.addToken(req, accessToken);
    }

    return next.handle(authReq).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 && !this.isExcludedUrl(req.url)) {
          return this.handle401Error(authReq, next);
        } else {
          return throwError(() => error);
        }
      })
    );
  }

  private addToken(request: HttpRequest<any>, token: string): HttpRequest<any> {
    return request.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  private isExcludedUrl(url: string): boolean {
    const apiBaseUrl = '';

    const excludedEndpoints = [
      `${apiBaseUrl}/api/Authentication/login`,
      `${apiBaseUrl}/api/Authentication/refresh-token`,
      `${apiBaseUrl}/api/Authentication/register`,
      `${apiBaseUrl}/api/Authentication/verify-user`,
      `${apiBaseUrl}/api/Authentication/password-reset/initiate`,
      `${apiBaseUrl}/api/Authentication/password-reset/reset`,
      `${apiBaseUrl}/api/Authentication/validate-password`,
      `${apiBaseUrl}/api/Authentication/check-username`,
      `${apiBaseUrl}/api/Authentication/check-email`,
      `${apiBaseUrl}/api/User/ProfilePicture`,
      `${apiBaseUrl}/api/Invite/is-invite-only`
      // Add other public endpoints as needed
    ];

    // Check if the URL matches any of the excluded endpoints
    return excludedEndpoints.some(endpoint => url === endpoint || url.startsWith(endpoint));
  }

  private handle401Error(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.refreshTokenInProgress) {
      this.refreshTokenInProgress = true;
      this.refreshTokenSubject.next(null); // Signal ongoing refresh, invalidate current token for queue

      this.authService.refreshToken().subscribe({ // Eagerly subscribe
        next: (res) => {
          this.refreshTokenInProgress = false;
          this.authService.setUser(res.user); // Assuming this is synchronous enough
          this.retryCount = 0; // Reset on success
          this.refreshTokenSubject.next(res.accessToken);
        },
        error: (err) => {
          this.refreshTokenInProgress = false;
          this.refreshTokenSubject.error(err); // Notify all listeners of the error
          // Re-initialize subject for any future independent refresh cycles
          this.refreshTokenSubject = new BehaviorSubject<string | null>(null);
          this.logout();
          // No return throwError here as this is a detached subscription's error handler
        }
      });
    }

    // All requests (initiating or queued) now consistently wait on the refreshTokenSubject
    return this.refreshTokenSubject.pipe(
      filter((token: string | null): token is string => token != null), // Type guard for clarity
      distinctUntilChanged(),
      take(1), // Process only one token emission
      switchMap(token => { // token is now definitely string
        return next.handle(this.addToken(req, token));
      }),
      catchError((err) => {
        // This catchError handles errors from refreshTokenSubject.error() or from next.handle() if it fails after retry
        // If the error came from refreshTokenSubject.error(), logout() was already called.
        // To avoid double logout or other side effects, we might need more specific error handling here.
        // For now, just rethrow. If logout wasn't called (e.g. next.handle failed), it should be.
        // This could be an issue if next.handle() fails with a non-401 after retry.
        if (!this.refreshTokenInProgress && !this.authService.getAccessToken()) { // A simple check, might need refinement
             // If refresh wasn't in progress (e.g. error from next.handle) AND there's no token, perhaps logout.
             // This part is tricky; for now, let's assume logout is handled by the refresh path or higher up.
        }
        return throwError(() => err);
      })
    );
  }

  private logout() {
    this.authService.logout();
    setTimeout(() => {
      this.router.navigate(['/']);
    }, 1000);
  }
}

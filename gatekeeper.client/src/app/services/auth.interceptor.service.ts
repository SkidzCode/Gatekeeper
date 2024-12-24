// src/app/interceptors/auth.interceptor.ts
import { Injectable } from '@angular/core';
import {
  HttpEvent, HttpInterceptor, HttpHandler, HttpRequest, HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private refreshTokenInProgress = false;
  private refreshTokenSubject: BehaviorSubject<string | null> = new BehaviorSubject<string | null>(null);

  constructor(private authService: AuthService) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Clone the request to add the new header
    let authReq = req;
    const accessToken = this.authService.getAccessToken();

    // Exclude certain URLs from having the Authorization header
    if (accessToken && !this.isExcludedUrl(req.url)) {
      authReq = this.addToken(req, accessToken);
    }

    return next.handle(authReq).pipe(
      catchError((error: HttpErrorResponse) => {
        // If unauthorized (401), attempt to refresh
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
    // Construct the full API base URL dynamically
    const apiBaseUrl = `${window.location.origin}`;

    // Define URLs that should be excluded from interception
    const excludedEndpoints = [
      `${apiBaseUrl}/api/Authentication/login`,
      `${apiBaseUrl}/api/Authentication/refresh-token`,
      `${apiBaseUrl}/api/Authentication/register`,
      `${apiBaseUrl}/api/Authentication/verify-user`,
      `${apiBaseUrl}/api/Authentication/password-reset/initiate`,
      `${apiBaseUrl}/api/Authentication/password-reset/reset`,
      `${apiBaseUrl}/api/Authentication/validate-password`,
      // Add other public endpoints as needed
    ];

    // Check if the URL matches any of the excluded endpoints
    return excludedEndpoints.some(endpoint => url === endpoint || url.startsWith(endpoint));
  }

  private handle401Error(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.refreshTokenInProgress) {
      this.refreshTokenInProgress = true;
      this.refreshTokenSubject.next(null);

      return this.authService.refreshToken().pipe(
        switchMap((res) => {
          this.refreshTokenInProgress = false;
          this.refreshTokenSubject.next(res.accessToken);
          this.authService.setUser(res.user);
          // Retry the failed request with the new token
          return next.handle(this.addToken(req, res.accessToken));
        }),
        catchError((err) => {
          this.refreshTokenInProgress = false;
          this.authService.logout();
          return throwError(() => err);
        })
      );
    } else {
      // Wait until the token is refreshed and retry the request
      return this.refreshTokenSubject.pipe(
        filter(token => token != null),
        take(1),
        switchMap(token => {
          return next.handle(this.addToken(req, token!));
        })
      );
    }
  }
}

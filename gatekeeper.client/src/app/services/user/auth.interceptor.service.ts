import { Injectable } from '@angular/core';
import {
  HttpEvent, HttpInterceptor, HttpHandler, HttpRequest, HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from './auth.service';
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
          this.retryCount = 0;
          return next.handle(this.addToken(req, res.accessToken));
        }),
        catchError((err) => {
          this.refreshTokenInProgress = false;
          this.retryCount++;
          this.logout();
          return throwError(() => err);
        })
      );
    } else {
      return this.refreshTokenSubject.pipe(
        filter(token => token != null),
        take(1),
        switchMap(token => {
          return next.handle(this.addToken(req, token!));
        })
      );
    }
  }

  private logout() {
    this.authService.logout();
    setTimeout(() => {
      this.router.navigate(['/']);
    }, 1000);
  }
}

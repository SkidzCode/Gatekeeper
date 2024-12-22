import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {

  constructor(private authService: AuthService, private router: Router) { }

  canActivate(): boolean {
    const accessToken = this.authService.getAccessToken();
    if (accessToken) {
      // Here you could also check if the token is not expired.
      // For simplicity, we just check for its presence.
      return true;
    } else {
      // No token present, so navigate to login
      this.router.navigate(['/login']);
      return false;
    }
  }
}

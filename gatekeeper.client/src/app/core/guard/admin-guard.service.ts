import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { AuthService } from '../services/user/auth.service';

@Injectable({
  providedIn: 'root',
})
export class AdminGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) { }

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    return this.authService.currentUser$.pipe(
      take(1), // unsubscribe automatically after the first emission
      map(user => {
        // Safely check user, user.roles, ensure it's an array, then call includes
        const isAdmin = !!(user && user.roles && Array.isArray(user.roles) && user.roles.includes('Admin'));
        if (!isAdmin) {
          // If the user isn’t an admin, redirect or do something else
          this.router.navigate(['/forbidden']); // or wherever you want to redirect
          return false;
        }
        return true;
      })
    );
  }
}

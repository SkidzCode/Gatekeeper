import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';

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
        const isAdmin = user?.roles.includes('Admin') || false;
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

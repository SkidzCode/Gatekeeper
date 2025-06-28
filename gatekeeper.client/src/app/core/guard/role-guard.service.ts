import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators'; // Import RxJS operators
import { AuthService } from '../services/user/auth.service';
import { User } from '../../shared/models/user.model'; // Corrected path based on guard location

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {

  constructor(private authService: AuthService, private router: Router) { }

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean | UrlTree> {

    const requiredRole = route.data['requiredRole'] as string;
    const routePath = route.pathFromRoot.map(r => r.url.join('/')).join('/');
    // console.log(`[RoleGuard] Checking access for route: ${routePath}, Required role: ${requiredRole}`); // Verbose, commented out

    return this.authService.currentUser$.pipe(
      take(1), // Take the first emitted value and then complete
      map((user: User | null) => {
        if (!requiredRole) {
          // console.log(`[RoleGuard] No specific role required for ${routePath}. Access granted.`); // Verbose, commented out
          return true;
        }

        if (!user || !user.roles || user.roles.length === 0) {
          console.warn(`[RoleGuard] User not authenticated or has no roles for route: ${routePath}. Redirecting to login.`);
          return this.router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
        }

        const userRoles: string[] = user.roles;
        // console.log(`[RoleGuard] User roles: ${userRoles.join(', ')} for route: ${routePath}`); // Verbose, commented out

        if (userRoles.includes(requiredRole)) {
          // console.log(`[RoleGuard] User has the required role: ${requiredRole} for route: ${routePath}. Access granted.`); // Verbose, commented out
          return true;
        } else if (requiredRole.toLowerCase() === 'user' && userRoles.map(r => r.toLowerCase()).includes('admin')) {
          // console.log(`[RoleGuard] User is Admin, granting access to "User" role restricted route: ${routePath}. Access granted.`); // Verbose, commented out
          return true;
        } else {
          console.warn(`[RoleGuard] User does not have the required role: ${requiredRole} for route: ${routePath}. User roles: ${userRoles.join(', ')}. Redirecting to home.`);
          return this.router.createUrlTree(['/']);
        }
      })
    );
  }
}

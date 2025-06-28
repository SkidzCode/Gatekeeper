import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { map, take, filter } from 'rxjs/operators'; // filter imported
import { AuthService } from '../services/user/auth.service';
import { User } from '../../shared/models/user.model';

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
    // console.log(`[RoleGuard] Evaluating for route: ${routePath}, Required role: ${requiredRole}`);

    return this.authService.currentUser$.pipe(
      filter((user: User | null): user is User => { // Type predicate 'user is User' helps TypeScript infer type in subsequent operators
        if (user === null || user === undefined) {
          // This case should ideally be handled by AuthGuard if it runs first.
          // If RoleGuard runs and user is null, it implies AuthGuard might not have run or passed unexpectedly.
          // console.log(`[RoleGuard] User is null/undefined for ${routePath}. Waiting for AuthGuard or valid user state.`);
          return false; // Continue waiting for a valid user object or for AuthGuard to redirect.
        }
        if (!user.roles || user.roles.length === 0) {
          // User object might be present, but roles are not yet populated.
          // console.log(`[RoleGuard] User ${user.username} present but has no roles yet for ${routePath}. Waiting for roles...`);
          return false; // Wait for roles to be populated.
        }
        // console.log(`[RoleGuard] User ${user.username} with roles ${user.roles.join(', ')} received for ${routePath}. Proceeding to role check.`);
        return true; // User is not null and has a non-empty roles array.
      }),
      take(1), // Take the first emission that passes the filter (i.e., user with roles)
      map((user: User) => { // user is guaranteed by the filter to be User (non-null with roles)
        // console.log(`[RoleGuard] Performing role check for ${user.username} against required role '${requiredRole}' for ${routePath}`);
        if (!requiredRole) {
          // console.log(`[RoleGuard] No specific role required for ${routePath}. Access granted.`);
          return true;
        }

        // The user.roles check from the filter ensures user.roles is available and not empty.
        const userRoles: string[] = user.roles;

        if (userRoles.includes(requiredRole)) {
          // console.log(`[RoleGuard] User has the required role: ${requiredRole} for ${routePath}. Access granted.`);
          return true;
        } else if (requiredRole.toLowerCase() === 'user' && userRoles.map(r => r.toLowerCase()).includes('admin')) {
          // console.log(`[RoleGuard] User is Admin, granting access to "User" role restricted route: ${routePath}. Access granted.`);
          return true;
        } else {
          console.warn(`[RoleGuard] User ${user.username} (Roles: ${userRoles.join(', ')}) does not have the required role: '${requiredRole}' for route: ${routePath}. Redirecting to home.`);
          return this.router.createUrlTree(['/']);
        }
      })
    );
  }
}

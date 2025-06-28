import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../services/user/auth.service'; // Assuming AuthService is where user roles can be accessed

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {

  constructor(private authService: AuthService, private router: Router) { }

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {

    const requiredRole = route.data['requiredRole'] as string;
    const routePath = route.pathFromRoot.map(r => r.url.join('/')).join('/');
    console.log(`[RoleGuard] Checking access for route: ${routePath}, Required role: ${requiredRole}`);

    if (!requiredRole) {
      console.log('[RoleGuard] No specific role required. Access granted.');
      return true; // No role specified on the route, so allow access
    }

    const currentUser = this.authService.currentUserValue; // Assuming a synchronous way to get current user value for guards

    if (!currentUser || !currentUser.roles || currentUser.roles.length === 0) {
      console.warn('[RoleGuard] User not authenticated or has no roles. Redirecting to login.');
      this.router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    const userRoles: string[] = currentUser.roles;
    console.log(`[RoleGuard] User roles: ${userRoles.join(', ')}`);

    // For now, a simple direct match.
    // Consider if "Admin" should grant access to "User" routes implicitly.
    // For example, if requiredRole is 'User', and userRoles includes 'Admin', should it pass?
    // Current logic: if requiredRole is 'User', user must have 'User' role explicitly.
    // If requiredRole is 'User', and user has 'Admin', 'User' -> this passes.
    // If requiredRole is 'Admin', and user has 'User' -> this fails.

    // A common scenario: if a route requires "User", an "Admin" should also pass.
    // Let's implement a check: if user is "Admin", they can access any route that requires "User".
    // More sophisticated role hierarchies would need a dedicated role service/logic.

    if (userRoles.includes(requiredRole)) {
      console.log(`[RoleGuard] User has the required role: ${requiredRole}. Access granted.`);
      return true;
    } else if (requiredRole.toLowerCase() === 'user' && userRoles.map(r => r.toLowerCase()).includes('admin')) {
      // Special case: Admins can access routes that require "User" role.
      console.log('[RoleGuard] User is Admin, granting access to "User" role restricted route. Access granted.');
      return true;
    } else {
      console.warn(`[RoleGuard] User does not have the required role: ${requiredRole}. User roles: ${userRoles.join(', ')}. Redirecting to home.`);
      this.router.navigate(['/']); // Or an 'access-denied' page
      return false;
    }
  }
}

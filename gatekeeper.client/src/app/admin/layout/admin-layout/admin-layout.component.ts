import { Component, inject } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Observable } from 'rxjs';
import { map, shareReplay } from 'rxjs/operators';
import { AuthService } from '../../../services/user/auth.service'; // Adjust the path to your AuthService
import { Router } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  standalone: false,

  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent {
  private breakpointObserver = inject(BreakpointObserver);
  isLoggedIn = false;
  isAdmin = false;
  username: string | null = null;

  opened: boolean = true;
  events: string[] = [];
  isHandset$: Observable<boolean> = this.breakpointObserver.observe(Breakpoints.Handset)
    .pipe(
      map(result => result.matches),
      shareReplay()
    );

  constructor(private authService: AuthService, private router: Router) {
    this.authService.currentUser$.subscribe((user) => {
      this.isLoggedIn = !!user; // True if user is logged in
      this.username = user?.username || null; // Get username if user exists
      this.isAdmin = user?.roles.includes('Admin') || false; // Check if user is admin
      console.log(`User Roles: ${user?.roles}`);
    });
  }

  logout(): void {
    this.authService.logout();
    this.isLoggedIn = false;
    this.username = null;
    this.router.navigate(['/login']);
  }
}

import { Component, inject, AfterViewInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatSidenavContainer } from '@angular/material/sidenav';
import { Observable } from 'rxjs';
import { map, shareReplay } from 'rxjs/operators';
import { AuthService } from '../../../core/services/user/auth.service'; // Adjust the path to your AuthService
import { Router } from '@angular/router';

@Component({
  selector: 'app-portal-layout',
  standalone: false,

  templateUrl: './portal-layout.component.html',
  styleUrl: './portal-layout.component.scss'
})
export class PortalLayoutComponent implements AfterViewInit {
  @ViewChild(MatSidenavContainer) sidenavContainer!: MatSidenavContainer;

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

  constructor(private authService: AuthService, private router: Router, private cdRef: ChangeDetectorRef) {
    this.authService.currentUser$.subscribe((user) => {
      this.isLoggedIn = !!user; // True if user is logged in
      this.username = user?.username || null; // Get username if user exists
      this.isAdmin = user?.roles.includes('Admin') || false; // Check if user is admin
      console.log(`User Roles: ${user?.roles}`);
    });
  }

  ngAfterViewInit() {
    // Let everything render first, then update the margins
    setTimeout(() => {
      if (this.sidenavContainer) {
        this.sidenavContainer.updateContentMargins();
        this.cdRef.detectChanges();
      }
    }, 1000);
  }

  logout(): void {
    this.authService.logoutCurrentSession().subscribe(message => {
      console.log(message);
      this.isLoggedIn = false;
      this.username = null;
      this.router.navigate(['/']);
    });
  }
}

import { Component, inject, AfterViewInit, ChangeDetectorRef, viewChild } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatSidenavContainer } from '@angular/material/sidenav';
import { Observable } from 'rxjs';
import { map, shareReplay } from 'rxjs/operators';
import { AuthService } from '../../../core/services/user/auth.service'; // Adjust the path to your AuthService
import { Router } from '@angular/router';

@Component({
  selector: 'app-main-layout',
  standalone: false,
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements AfterViewInit {
  readonly sidenavContainer = viewChild.required(MatSidenavContainer);

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
      const sidenavContainer = this.sidenavContainer();
      if (sidenavContainer) {
        sidenavContainer.updateContentMargins();
        this.cdRef.detectChanges();
      }
    }, 1000);
  }
}

import { Component, inject, AfterViewInit, ChangeDetectorRef, viewChild } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatSidenavContainer, MatSidenavModule } from '@angular/material/sidenav';
import { Observable } from 'rxjs';
import { map, shareReplay } from 'rxjs/operators';
import { AuthService } from '../../../core/services/user/auth.service'; // Adjust the path to your AuthService
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

import { PluginLoaderService } from '../../../core/services/plugin-loader.service';
import { AngularPluginInfo } from '../../../core/models/plugin-info.model';

@Component({
  selector: 'app-portal-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule
  ],
  templateUrl: './portal-layout.component.html',
  styleUrl: './portal-layout.component.scss'
})
export class PortalLayoutComponent implements AfterViewInit {
  readonly sidenavContainer = viewChild.required(MatSidenavContainer);

  private breakpointObserver = inject(BreakpointObserver);
  isLoggedIn = false;
  isAdmin = false;
  username: string | null = null;
  pluginNavLinks: AngularPluginInfo[] = [];

  opened: boolean = true;
  events: string[] = [];
  isHandset$: Observable<boolean> = this.breakpointObserver.observe(Breakpoints.Handset)
    .pipe(
      map(result => result.matches),
      shareReplay()
    );

  constructor(private authService: AuthService, private router: Router, private cdRef: ChangeDetectorRef, private pluginLoader: PluginLoaderService) {
    this.authService.currentUser$.subscribe((user) => {
      this.isLoggedIn = !!user; // True if user is logged in
      this.username = user?.username || null; // Get username if user exists
      this.isAdmin = user?.roles.includes('Admin') || false; // Check if user is admin
      console.log(`User Roles: ${user?.roles}`);
    });
    this.pluginNavLinks = this.pluginLoader.getPluginManifests();
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

  logout(): void {
    this.authService.logoutCurrentSession().subscribe(message => {
      console.log(message);
      this.isLoggedIn = false;
      this.username = null;
      this.router.navigate(['/']);
    });
  }
}

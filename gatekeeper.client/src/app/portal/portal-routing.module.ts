/// <reference path="../admin/admin-routing.module.ts" />
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Layout
import { PortalLayoutComponent } from './layout/portal-layout/portal-layout.component';

// Components
import { HomeComponent } from './home/home/home.component';
import { UserProfileComponent } from './user/user-profile/user-profile.component';
import { UserSettingsComponent } from './user/user-settings/user-settings.component'
import { InviteComponent } from './user/invite/invite.component'
import { SessionsComponent } from './user/sessions/sessions.component';

// Guard
import { DisabledGuardService } from '../core/guard/disabled-guard.service';
import { AuthGuard } from '../core/guard/auth-guard.service';


const routes: Routes = [
  {
    path: '', // Added 'portal' path prefix
    component: PortalLayoutComponent,
    children: [
      { path: '', component: HomeComponent, canActivate: [DisabledGuardService, AuthGuard] },
      { path: 'user/profile', component: UserProfileComponent, canActivate: [DisabledGuardService, AuthGuard] },
      { path: 'user/settings', component: UserSettingsComponent, canActivate: [DisabledGuardService, AuthGuard] },
      { path: 'user/invite', component: InviteComponent, canActivate: [DisabledGuardService, AuthGuard] },
      { path: 'user/sessions', component: SessionsComponent, canActivate: [DisabledGuardService, AuthGuard] },
      // ... more portal routes here
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PortalRoutingModule { }

import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Layout
import { PortalLayoutComponent } from './layout/portal-layout/portal-layout.component';

// Components
import { HomeComponent } from './home/home/home.component'; 
import { UserProfileComponent } from './user/user-profile/user-profile.component';
import { UserSettingsComponent } from './user/user-settings/user-settings.component'
import { InviteComponent } from './user/invite/invite.component'


// Guard
import { DisabledGuardService } from '../services/guard/disabled-guard.service';
import { AuthGuard } from '../services/guard/auth-guard.service';

const routes: Routes = [
  {
    path: '',
    component: PortalLayoutComponent, 
    children: [
      { path: '', component: HomeComponent, canActivate: [DisabledGuardService, AuthGuard] },
      { path: 'user/profile', component: UserProfileComponent, canActivate: [DisabledGuardService, AuthGuard] },
      { path: 'user/settings', component: UserSettingsComponent, canActivate: [DisabledGuardService, AuthGuard] },
      { path: 'user/invite', component: InviteComponent, canActivate: [DisabledGuardService, AuthGuard] }
      // ... more portal routes here
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PortalRoutingModule { }

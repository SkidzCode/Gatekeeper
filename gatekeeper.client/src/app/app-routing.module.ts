import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Layouts
import { MainLayoutComponent } from './site/layout/main-layout/main-layout.component';
// (If you don't have a separate layout component for the main area yet,
//  you can create one. Alternatively, you could keep using AppComponent,
//  but a dedicated layout component is cleaner.)

// Components
import { DisabledComponent } from './site/disabled/disabled.component'; 
import { UserLoginComponent } from './user/user-login/user-login.component';
import { ForgotPasswordComponent } from './user/passwords/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './user/passwords/reset-password/reset-password.component';
import { UserProfileComponent } from './user/user-profile/user-profile.component';
import { UserRegisterComponent } from './user/user-register/user-register.component';
import { UserVerifyComponent } from './user/user-verify/user-verify.component';
import { ResourcesEditorComponent } from './resources/resources-editor/resources-editor.component';

// Guards
import { AuthGuard } from './services/guard/auth-guard.service';
import { DisabledGuardService } from './services/guard/disabled-guard.service';

const routes: Routes = [
  // Main layout routes
  {
    path: '',
    component: MainLayoutComponent, // <-- your main layout for non-admin
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'disabled', component: DisabledComponent }, 
      { path: 'login', component: UserLoginComponent, canActivate: [DisabledGuardService] },
      { path: 'forgot', component: ForgotPasswordComponent, canActivate: [DisabledGuardService] },
      { path: 'resources', component: ResourcesEditorComponent, canActivate: [DisabledGuardService, AuthGuard] },
      { path: 'user/profile', component: UserProfileComponent, canActivate: [DisabledGuardService, AuthGuard] },
      { path: 'reset', component: ResetPasswordComponent, canActivate: [DisabledGuardService] },
      { path: 'register', component: UserRegisterComponent, canActivate: [DisabledGuardService] },
      { path: 'verify', component: UserVerifyComponent, canActivate: [DisabledGuardService] }
      // ... any other non-admin routes
    ],
  },

  // Lazy-loaded Admin module
  // We'll show an example if you want to handle
  // the layout inside the AdminModule itself.
  {
    path: 'admin',
    loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule),
    // If you prefer the AdminLayout at this level, you could do:
    // component: AdminLayoutComponent,
    // children: [ { path: '', loadChildren: ... } ]
  },

  // Wildcard / fallback
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule { }

import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UserLoginComponent } from './user/user-login/user-login.component';
import { AuthGuard } from './services/auth-guard.service';
import { ResourcesEditorComponent } from './resources/resources-editor/resources-editor.component';
import { ResetPasswordComponent } from './user/passwords/reset-password/reset-password.component';

import { ForgotPasswordComponent } from './user/passwords/forgot-password/forgot-password.component';
import { UserProfileComponent } from './user/user-profile/user-profile.component';
import { UserRegisterComponent } from './user/user-register/user-register.component';
import { UserVerifyComponent } from './user/user-verify/user-verify.component';

const routes: Routes = [
  {
    path: 'admin',
    loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule),
  },
  { path: 'login', component: UserLoginComponent },
  { path: 'forgot', component: ForgotPasswordComponent },
  { path: 'resources', component: ResourcesEditorComponent, canActivate: [AuthGuard] },
  { path: 'user/profile', component: UserProfileComponent, canActivate: [AuthGuard] },
  { path: 'reset', component: ResetPasswordComponent },
  { path: 'register', component: UserRegisterComponent },
  { path: 'verify', component: UserVerifyComponent }

];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }

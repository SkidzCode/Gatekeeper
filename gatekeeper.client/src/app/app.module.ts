import { NgModule } from '@angular/core';
import { MatOptionModule } from '@angular/material/core';
import { MatDialogModule } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';

import { UserLoginComponent } from './user/user-login/user-login.component';
import { ForgotPasswordComponent } from './user/passwords/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './user/passwords/reset-password/reset-password.component';
import { UserRegisterComponent } from './user/user-register/user-register.component';
import { UserVerifyComponent } from './user/user-verify/user-verify.component';
import { MainLayoutComponent } from './site/layout/main-layout/main-layout.component';

import { AdminLayoutComponent } from './site/layout/admin-layout/admin-layout.component';
import { LoggedInComponent } from './site/layout/logged-in/logged-in.component';

import { AuthInterceptor } from './services/user/auth.interceptor.service';
import { DisabledComponent } from './site/disabled/disabled.component';
import { HomeComponent } from './site/home/home/home.component';

@NgModule({
  declarations: [
    AppComponent,
    UserLoginComponent,
    ForgotPasswordComponent,
    UserRegisterComponent,
    ResetPasswordComponent,
    UserVerifyComponent,
    MainLayoutComponent,
    DisabledComponent,
    LoggedInComponent,
    AdminLayoutComponent,
    HomeComponent
  ],
  imports: [
    ReactiveFormsModule,
    HttpClientModule,
    BrowserAnimationsModule,
    MatInputModule,
    MatButtonModule,
    MatFormFieldModule,
    AppRoutingModule,
    MatCardModule,
    MatIconModule,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    FormsModule,
    MatDialogModule,
    MatOptionModule
  ],
  providers: [
    provideAnimationsAsync(),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }

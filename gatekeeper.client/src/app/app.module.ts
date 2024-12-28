

import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { UserLoginComponent } from './user/user-login/user-login.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';


import { ReactiveFormsModule, FormsModule, FormGroup } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';

import { AuthService } from './services/user/auth.service'; // Ensure the path is correct
import { AuthInterceptor } from './services/user/auth.interceptor.service';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatCardModule } from '@angular/material/card';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import {MatSelectModule} from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialogContainer, MatDialogModule } from '@angular/material/dialog'
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatOptionModule } from '@angular/material/core';
import { MatRadioModule } from '@angular/material/radio';


import { ForgotPasswordComponent } from './user/passwords/forgot-password/forgot-password.component';
import { ResourcesEditorComponent } from './resources/resources-editor/resources-editor.component';
import { ResetPasswordComponent } from './user/passwords/reset-password/reset-password.component';
import { UserProfileComponent } from './user/user-profile/user-profile.component';
import { UserRegisterComponent } from './user/user-register/user-register.component';
import { UserVerifyComponent } from './user/user-verify/user-verify.component';
import { MainLayoutComponent } from './site/layout/main-layout/main-layout.component';
import { AdminLayoutComponent } from './site/layout/admin-layout/admin-layout.component';
import { DisabledComponent } from './site/disabled/disabled.component';
import { UserSettingsComponent } from './user/user-settings/user-settings.component';
@NgModule({
  declarations: [
    AppComponent,
    UserLoginComponent,
    ForgotPasswordComponent,
    ResourcesEditorComponent,
    UserRegisterComponent,
    ResetPasswordComponent,
    UserProfileComponent,
    UserVerifyComponent,
    MainLayoutComponent,
    AdminLayoutComponent,
    DisabledComponent,
    UserSettingsComponent
  ],
  imports: [
    BrowserModule,
    ReactiveFormsModule,
    HttpClientModule,
    BrowserAnimationsModule,
    MatInputModule,
    MatButtonModule,
    MatFormFieldModule,
    AppRoutingModule,
    MatGridListModule,
    MatCardModule,
    MatMenuModule,
    MatIconModule,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatProgressSpinner,
    FormsModule,
    MatTableModule,
    MatSelectModule,
    MatSnackBarModule,
    MatDividerModule,
    MatDialogContainer,
    MatDialogModule,
    MatPaginatorModule,
    MatSortModule,
    MatOptionModule,
    MatRadioModule
    
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

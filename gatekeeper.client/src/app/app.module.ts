

import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { LoginComponent } from './login/login.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';


import { ReactiveFormsModule, FormsModule, FormGroup } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';

import { AuthService } from './services/auth.service'; // Ensure the path is correct
import { AuthInterceptor } from './services/auth.interceptor.service';
import { DashBoardComponent } from './dash-board/dash-board.component';
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

import { ForgotPasswordComponent } from './passwords/forgot-password/forgot-password.component';
import { ResourcesEditorComponent } from './resources/resources-editor/resources-editor.component';
import { PasswordRecoveryComponent } from './passwords/password-recovery/password-recovery.component';
import { ResetPasswordComponent } from './passwords/reset-password/reset-password.component';
import { UserProfileComponent } from './user/user-profile/user-profile.component';
import { UserRegisterComponent } from './user/user-register/user-register.component';
import { UserVerifyComponent } from './user/user-verify/user-verify.component';
@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    DashBoardComponent,
    ForgotPasswordComponent,
    ResourcesEditorComponent,
    PasswordRecoveryComponent,
    UserRegisterComponent,
    ResetPasswordComponent,
    UserProfileComponent,
    UserVerifyComponent
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
    MatDialogModule 
    
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

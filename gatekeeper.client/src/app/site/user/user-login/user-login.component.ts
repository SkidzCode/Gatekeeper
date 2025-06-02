import { Component } from '@angular/core';
import { AuthService } from '../../../core/services/user/auth.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  standalone: false, // Explicitly specifying this to meet your requirements
  selector: 'app-user-login',
  templateUrl: './user-login.component.html',
  styleUrls: ['./user-login.component.scss'],
  
})
export class UserLoginComponent {
  loginForm: FormGroup;
  errorMessage: string = '';

  constructor(private fb: FormBuilder, private authService: AuthService, private router: Router) {
    this.loginForm = this.fb.group({
      identifier: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(4)]],
    });
    this.authService.currentUser$.subscribe((user) => {
      if (!!user)
        this.router.navigate(['/portal']);
    });
  }

  onSubmit(): void {
    if (this.loginForm.valid) {
      const { identifier, password } = this.loginForm.value;

      this.authService.login(identifier, password).subscribe({
        next: (response) => {
          console.log('Login successful:', response);
          // Navigate to protected page now that we have a token
          this.router.navigate(['/portal']);
        },
        error: (error) => {
          console.error('Login failed:', error);
          if (error.status === 401 || error.status === 403) {
            if (error.error && error.error.Message) {
              this.errorMessage = error.error.Message;
            } else {
              this.errorMessage = 'Login failed. Please check your username and password, or contact support if you believe your access is restricted.';
            }
          } else if (error.status === 429) {
            if (error.error && error.error.Message) {
              this.errorMessage = error.error.Message; // This should be the "Account locked..." message
            } else {
              this.errorMessage = 'Your account is temporarily locked due to too many failed login attempts. Please try again later.';
            }
          } else {
            this.errorMessage = 'An unexpected error occurred during login. Please try again.';
          }
        }
      });
    }
  }
}

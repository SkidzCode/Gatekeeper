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
          // Logging lines (can be kept for now or removed if confident)
          console.log('Full error object:', JSON.stringify(error, Object.getOwnPropertyNames(error)));
          console.log('Error status raw:', error.status);
          console.log('Error status type:', typeof error.status);
          console.log('error.error value:', JSON.stringify(error.error));
          console.error('Login failed (original log):', error);

          // Default error message
          let specificMessageFound = false;

          if (error.status === 401 || error.status === 403) {
            if (error.error && typeof error.error === 'object' && error.error.Message) {
              this.errorMessage = error.error.Message;
              specificMessageFound = true;
            } else {
              // Fallback for 401/403 if specific message structure isn't there
              this.errorMessage = 'Login failed. Please check your username and password, or contact support if you believe your access is restricted.';
            }
          } else if (error.status === 429) {
            if (error.error && typeof error.error === 'object' && error.error.Message) {
              this.errorMessage = error.error.Message; // Expecting "Account locked..."
              specificMessageFound = true;
            } else {
              // Fallback for 429 if specific message structure isn't there
              this.errorMessage = 'Your account is temporarily locked due to too many failed login attempts. Please try again later.';
            }
          } else {
            // For other errors, or if error.status is not available (though it should be now)
            if (error.error && typeof error.error === 'object' && error.error.Message) {
                this.errorMessage = error.error.Message; // Handle cases where status might be missing but message exists
            } else if (typeof error.message === 'string') { // Fallback to error.message if it's a string (less likely now)
                this.errorMessage = error.message;
            }
            else {
                this.errorMessage = 'An unexpected error occurred during login. Please try again.';
            }
          }
        }
      });
    }
  }
}

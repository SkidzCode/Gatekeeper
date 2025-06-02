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
          if (error.status === 429) {
            // Assuming the server sends a JSON response like { "Message": "Specific message from server" }
            // The AccountLockedException message is "Account locked. Try again in X minutes."
            // or "Too many login attempts. Account locked for X minutes."
            if (error.error && error.error.Message) {
              this.errorMessage = error.error.Message;
            } else {
              // Fallback if the detailed message structure is not as expected
              this.errorMessage = "You have exceeded the maximum number of login attempts. Please try again after some time.";
            }
          } else {
            this.errorMessage = "Login failed. Please check your username and password and try again.";
          }
        },
      });
    }
  }
}

import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors, AsyncValidatorFn } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Observable, of } from 'rxjs';
import { map, catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  username: string;
  phone: string;
  password: string;
  website: string;
}

@Component({
  selector: 'app-user-register',
  templateUrl: './user-register.component.html',
  styleUrls: ['./user-register.component.scss'],
  standalone: false,
})
export class UserRegisterComponent implements OnInit {
  registerForm!: FormGroup;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) { }

  ngOnInit(): void {
    // Initialize the registration form with validators
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email],
        [this.emailTakenValidator()]
      ],
      username: ['',
        [Validators.required, Validators.minLength(4)],
        [this.usernameTakenValidator()]
      ],
      phone: ['', [Validators.required, Validators.pattern(/^[0-9]{10,15}$/)]],
      newPassword: ['',
        [Validators.required, Validators.minLength(8)],
        [this.passwordStrengthValidator()]
      ],
      confirmPassword: ['', [Validators.required]]
    }, { validators: [this.passwordMatchValidator] });

    // Optionally, subscribe to password changes to display strength messages
    this.registerForm.get('newPassword')?.valueChanges.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(password => {
      // Password strength is handled by the validator
    });
  }

  /**
   * Asynchronous validator to check password strength.
   */
  passwordStrengthValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      const password = control.value;

      if (!password || password.length < 8) {
        // If password is empty or too short, no need to check strength
        return of(null);
      }

      return of(password).pipe(
        debounceTime(500),
        distinctUntilChanged(),
        switchMap((pwd) => this.authService.validatePasswordStrength(pwd)),
        map(response => {
          return response.isValid ? null : { weakPassword: true };
        }),
        catchError(() => of({ passwordStrengthCheckFailed: true }))
      );
    };
  }


  /**
   * Asynchronous validator to check if the username is already taken.
   */
  // user-register.component.ts

  usernameTakenValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      const username = control.value;

      if (!username || username.length < 4) {
        // If username is empty or too short, no need to check
        return of(null);
      }

      return of(username).pipe(
        debounceTime(500),
        distinctUntilChanged(),
        switchMap((uname) => this.authService.checkUsernameTaken(uname)),
        map(isTaken => {
          return isTaken ? { usernameTaken: true } : null;
        }),
        catchError(() => of(null)) // In case of error, don't block the user
      );
    };
  }

  /**
 * Asynchronous validator to check if the username is already taken.
 */
  // user-register.component.ts

  emailTakenValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      const email = control.value;

      if (!email || email.length < 4) {
        // If email is empty or too short, no need to check
        return of(null);
      }

      return of(email).pipe(
        debounceTime(500),
        distinctUntilChanged(),
        switchMap((uname) => this.authService.checkEmailTaken(uname)),
        map(isTaken => {
          return isTaken ? { emailTaken: true } : null;
        }),
        catchError(() => of(null)) // In case of error, don't block the user
      );
    };
  }


  /**
   * Validator to ensure that newPassword and confirmPassword match.
   */
  passwordMatchValidator(formGroup: FormGroup): ValidationErrors | null {
    const newPassword = formGroup.get('newPassword')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;
    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  }

  /**
   * Handle form submission.
   */
  onSubmit(): void {
    if (this.registerForm.invalid) return;

    const registerData: RegisterRequest = {
      firstName: this.registerForm.get('firstName')?.value,
      lastName: this.registerForm.get('lastName')?.value,
      email: this.registerForm.get('email')?.value,
      username: this.registerForm.get('username')?.value,
      phone: this.registerForm.get('phone')?.value,
      password: this.registerForm.get('newPassword')?.value,
      website: window.location.origin
    };

    this.authService.register(registerData)
      .subscribe({
        next: (res) => {
          this.successMessage = res.message || 'Registration successful!';
          this.errorMessage = '';
          // Optionally, navigate to login after a short delay
          // setTimeout(() => {
          //   this.router.navigate(['/login']);
          // }, 3000);
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'An error occurred during registration.';
          this.successMessage = '';
        }
      });
  }

  /**
   * Navigate to the login page.
   */
  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }
}

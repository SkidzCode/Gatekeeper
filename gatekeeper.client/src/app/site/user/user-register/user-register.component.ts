import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors, AsyncValidatorFn } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/user/auth.service';
import { Observable, of, Subscription } from 'rxjs';
import { map, catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  username: string;
  phone: string;
  password: string;
  website: string;
  userLicAgreement: boolean;
  receiveEmails: boolean;
}

@Component({
  selector: 'app-user-register',
  templateUrl: './user-register.component.html',
  styleUrls: ['./user-register.component.scss'],
  standalone:false
})
export class UserRegisterComponent implements OnInit, OnDestroy {
  registerForm!: FormGroup;
  errorMessage: string = '';
  successMessage: string = '';
  passwordSubscription!: Subscription | undefined;
  showEULA: boolean = false; // Controls EULA visibility

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) { }

  ngOnInit(): void {
    // Initialize the registration form with validators
    this.registerForm = this.fb.group(
      {
        firstName: ['', [Validators.required, Validators.minLength(2)]],
        lastName: ['', [Validators.required, Validators.minLength(2)]],
        email: [
          '',
          [Validators.required, Validators.email],
          [
            this.checkIfTaken(
              this.authService.checkEmailTaken.bind(this.authService),
              'emailTaken'
            ),
          ],
        ],
        username: [
          '',
          [Validators.required, Validators.minLength(4)],
          [
            this.checkIfTaken(
              this.authService.checkUsernameTaken.bind(this.authService),
              'usernameTaken'
            ),
          ],
        ],
        phone: ['', [Validators.required, Validators.pattern(/^[0-9]{10,15}$/)]],
        newPassword: [
          '',
          [Validators.required, Validators.minLength(8)],
          [this.passwordStrengthValidator()],
        ],
        confirmPassword: ['', [Validators.required]],
        userLicAgreement: [false, [Validators.requiredTrue]],
        receiveEmails: [true], // Set to true by default
      },
      { validators: [this.passwordMatchValidator] }
    );

    // Subscribe to newPassword value changes
    this.passwordSubscription = this.registerForm
      .get('newPassword')
      ?.valueChanges.pipe(debounceTime(500), distinctUntilChanged())
      .subscribe((password) => {
        // Optional: Handle password changes
      });
  }

  ngOnDestroy(): void {
    // Unsubscribe to prevent memory leaks
    if (this.passwordSubscription) {
      this.passwordSubscription.unsubscribe();
    }
  }

  /**
   * Toggle EULA visibility
   */
  toggleEULA(): void {
    this.showEULA = !this.showEULA;
  }

  /**
   * Asynchronous validator to check password strength.
   */
  passwordStrengthValidator(): AsyncValidatorFn {
    return (
      control: AbstractControl
    ): Observable<ValidationErrors | null> => {
      const password = control.value;

      if (!password || password.length < 8) {
        // If password is empty or too short, no need to check strength
        return of(null);
      }

      return of(password).pipe(
        debounceTime(500),
        distinctUntilChanged(),
        switchMap((pwd) => this.authService.validatePasswordStrength(pwd)),
        map((response) => {
          return response.isValid ? null : { weakPassword: true };
        }),
        catchError(() => of({ passwordStrengthCheckFailed: true }))
      );
    };
  }

  /**
   * Asynchronous validator to check if email or username is taken.
   */
  checkIfTaken(
    serviceMethod: (value: string) => Observable<boolean>,
    errorKey: string
  ): AsyncValidatorFn {
    return (
      control: AbstractControl
    ): Observable<ValidationErrors | null> => {
      const value = control.value;
      if (!value || value.length < 4) {
        // If email/username is empty or too short, no need to check
        return of(null);
      }
      return of(value).pipe(
        debounceTime(500),
        distinctUntilChanged(),
        switchMap(serviceMethod),
        map((isTaken) => (isTaken ? { [errorKey]: true } : null)),
        catchError(() => of(null))
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
      website: window.location.origin,
      userLicAgreement: this.registerForm.get('userLicAgreement')?.value,
      receiveEmails: this.registerForm.get('receiveEmails')?.value,
    };

    this.authService.register(registerData).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Registration successful!';
        this.errorMessage = '';
      },
      error: (err) => {
        this.errorMessage =
          err.error?.message || 'An error occurred during registration.';
        this.successMessage = '';
      },
    });
  }

  /**
   * Navigate to the login page.
   */
  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }
}

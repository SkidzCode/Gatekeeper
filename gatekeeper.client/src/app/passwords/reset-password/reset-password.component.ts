import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors, AsyncValidatorFn } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Observable, of } from 'rxjs';
import { map, catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css'],
  standalone: false,
})
export class ResetPasswordComponent implements OnInit {
  resetPasswordForm!: FormGroup;
  errorMessage: string = '';
  successMessage: string = '';
  token: string = '';

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private authService: AuthService,
    private router: Router
  ) { }

  ngOnInit(): void {
    // Retrieve the token from query parameters
    this.route.queryParams.subscribe(params => {
      this.token = params['token'] ? decodeURIComponent(params['token']) : '';
    });

    // Initialize the form with both synchronous and asynchronous validators
    this.resetPasswordForm = this.fb.group({
      newPassword: ['',
        [Validators.required, Validators.minLength(8)],
        [this.passwordStrengthValidator()]
      ],
      confirmPassword: ['', [Validators.required]]
    }, { validators: [this.passwordMatchValidator] });

    // Optionally, subscribe to password changes to display strength messages
    this.resetPasswordForm.get('newPassword')?.valueChanges.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(password => {
      // No need to manually check strength here as the validator handles it
      // But you can trigger UI updates or additional logic if needed
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
    if (this.resetPasswordForm.invalid) return;

    const newPassword = this.resetPasswordForm.get('newPassword')?.value;
    this.authService.resetPassword({ resetToken: this.token, newPassword })
      .subscribe({
        next: (res) => {
          this.successMessage = 'Your password has been successfully reset!';
          this.errorMessage = '';
          // Navigate to login after a short delay
          setTimeout(() => {
            this.router.navigate(['/login']);
          }, 3000);
        },
        error: (err) => {
          this.errorMessage = err;
          this.successMessage = '';
        }
      });
  }
}

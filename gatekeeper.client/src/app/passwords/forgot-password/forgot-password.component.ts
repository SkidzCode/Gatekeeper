import { Component, OnInit, TemplateRef, ViewChild, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { Subscription } from 'rxjs';

interface PasswordResetInitiateRequest {
  emailOrUsername: string;
  website: string;
}

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css'],
  standalone: false
})
export class ForgotPasswordComponent implements OnInit, OnDestroy {
  @ViewChild('dialogTemplate') dialogTemplate!: TemplateRef<any>;

  forgotPasswordForm: FormGroup;
  isLoggedIn = false;
  username: string | null = null;
  successMessage: string | null = null;
  errorMessage: string | null = null;
  isProcessing = false; // Indicates if a request is in progress
  buttonHidden = false; // Controls the visibility of the Reset Password button

  private userSub!: Subscription;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private dialog: MatDialog
  ) {
    this.forgotPasswordForm = this.fb.group({
      emailOrUsername: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.userSub = this.authService.currentUser$.subscribe((user) => {
      this.isLoggedIn = !!user;
      this.username = user?.username || null;
    });
  }

  ngOnDestroy(): void {
    if (this.userSub) {
      this.userSub.unsubscribe();
    }
  }

  /**
   * Handles the click event on the Reset Password button.
   * Disables the button and initiates the password reset process based on user authentication.
   */
  onButtonClick(): void {
    if (this.isLoggedIn && this.username) {
      this.sendResetLink(this.username);
    } else {
      this.openDialog();
    }
  }

  /**
   * Opens the password reset dialog for guests.
   */
  openDialog(): void {
    const dialogRef: MatDialogRef<any> = this.dialog.open(this.dialogTemplate, {
      width: '400px',
      panelClass: 'custom-dialog-container'
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result === 'success') {
        this.successMessage = 'A password reset link has been dispatched. Check your inbox!';
        this.buttonHidden = true; // Hide the button after successful reset
      } else {
        this.isProcessing = false; // Re-enable the button if dialog was closed without success
      }
    });
  }

  /**
   * Handles the form submission within the dialog.
   */
  onDialogSubmit(): void {
    if (this.forgotPasswordForm.invalid || this.isProcessing) return;
    this.isProcessing = true; // Disable the button during processing

    const { emailOrUsername } = this.forgotPasswordForm.value;
    this.successMessage = null;
    this.errorMessage = null;
    const body: PasswordResetInitiateRequest = {
      emailOrUsername: emailOrUsername,
      website: window.location.origin
    };
    this.authService.initiatePasswordReset(body).subscribe({
      next: (response) => {
        this.forgotPasswordForm.reset();
        this.dialog.closeAll();
        this.successMessage = response.message || 'Reset link sent successfully!';
        this.buttonHidden = true; // Hide the button after successful reset
      },
      error: (error) => {
        this.errorMessage = error || 'An error occurred. Please try again.';
        this.isProcessing = false; // Re-enable the button if there's an error
      }
    });
  }

  /**
   * Closes the dialog without performing any action.
   */
  closeDialog(): void {
    this.dialog.closeAll();
  }

  /**
   * Sends the password reset link for logged-in users.
   * @param emailOrUsername The email or username of the user.
   */
  private sendResetLink(emailOrUsername: string): void {
    this.successMessage = null;
    this.errorMessage = null;

    const body: PasswordResetInitiateRequest = {
      emailOrUsername: emailOrUsername,
      website: window.location.origin
    };

    this.authService.initiatePasswordReset(body).subscribe({
      next: (response) => {
        this.successMessage = response.message || 'Reset link sent successfully!';
        this.buttonHidden = true; // Hide the button after successful reset
        this.isProcessing = false; // Re-enable if necessary
      },
      error: (error) => {
        this.errorMessage = error || 'An error occurred. Please try again.';
        this.isProcessing = false; // Re-enable the button if there's an error
      }
    });
  }
}

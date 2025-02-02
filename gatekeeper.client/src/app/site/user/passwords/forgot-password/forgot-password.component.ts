import { Component, OnInit, OnDestroy, TemplateRef, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/services/user/auth.service';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { Subscription } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';

interface PasswordResetInitiateRequest {
  emailOrUsername: string;
  website: string;
}

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss'],
  standalone: false
})
export class ForgotPasswordComponent implements OnInit, OnDestroy {
  @ViewChild('dialogTemplate') dialogTemplate!: TemplateRef<any>;

  forgotPasswordForm: FormGroup;
  isLoggedIn = false;
  username: string | null = null;
  isProcessing = false; // Indicates if a request is in progress
  buttonHidden = false; // Controls the visibility of the Reset Password button

  private userSub!: Subscription;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
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

  onButtonClick(): void {
    if (this.isLoggedIn && this.username) {
      this.sendResetLink(this.username);
    } else {
      this.openDialog();
    }
  }

  openDialog(): void {
    const dialogRef: MatDialogRef<any> = this.dialog.open(this.dialogTemplate, {
      width: '400px',
      panelClass: 'custom-dialog-container'
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result === 'success') {
        this.showSuccess('A password reset link has been dispatched. Check your inbox!');
        this.buttonHidden = true;
      } else {
        this.isProcessing = false;
      }
    });
  }

  onDialogSubmit(): void {
    if (this.forgotPasswordForm.invalid || this.isProcessing) return;
    this.isProcessing = true;

    const { emailOrUsername } = this.forgotPasswordForm.value;
    const body: PasswordResetInitiateRequest = {
      emailOrUsername: emailOrUsername,
      website: window.location.origin
    };

    this.authService.initiatePasswordReset(body).subscribe({
      next: (response) => {
        this.forgotPasswordForm.reset();
        this.dialog.closeAll();
        this.showSuccess(response.message || 'Reset link sent successfully!');
        this.buttonHidden = true;
      },
      error: (error) => {
        this.showError(error || 'An error occurred. Please try again.');
        this.isProcessing = false;
      }
    });
  }

  closeDialog(): void {
    this.dialog.closeAll();
  }

  private sendResetLink(emailOrUsername: string): void {
    const body: PasswordResetInitiateRequest = {
      emailOrUsername: emailOrUsername,
      website: window.location.origin
    };

    this.authService.initiatePasswordReset(body).subscribe({
      next: (response) => {
        this.showSuccess(response.message || 'Reset link sent successfully!');
        this.buttonHidden = true;
        this.isProcessing = false;
      },
      error: (error) => {
        this.showError(error || 'An error occurred. Please try again.');
        this.isProcessing = false;
      }
    });
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, '', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  private showError(message: string): void {
    this.snackBar.open(message, '', {
      duration: 3000,
      panelClass: ['error-snackbar']
    });
  }
}

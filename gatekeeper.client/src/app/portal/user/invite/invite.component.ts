import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Invite, InviteService } from '../../../core/services/user/invite.service';

// Example user retrieval from localStorage, or wherever your FromId is stored
interface CurrentUser {
  id: number;
  username: string;
  // ... other props
}

@Component({
  selector: 'app-invite',
  templateUrl: './invite.component.html',
  styleUrls: ['./invite.component.scss'],
  standalone: false
})
export class InviteComponent implements OnInit {
  inviteForm: FormGroup;
  myInvites: Invite[] = [];
  currentUser: CurrentUser | null = null;

  displayedColumns: string[] = [
    'toName',
    'toEmail',
    'isExpired',
    'isRevoked',
    'isComplete',
    'isSent'
  ];


  constructor(
    private fb: FormBuilder,
    private inviteService: InviteService,
    private snackBar: MatSnackBar
  ) {
    this.inviteForm = this.fb.group({
      toName: ['', Validators.required],
      toEmail: ['', [Validators.required, Validators.email]]
    });
  }

  ngOnInit(): void {
    this.currentUser = this.getCurrentUser();
    if (this.currentUser) {
      // Load invites if we have a user
      this.loadMyInvites(this.currentUser.id);
    }
  }

  /**
   * Retrieve current user from localStorage (or your AuthService).
   */
  private getCurrentUser(): CurrentUser | null {
    const storedUser = localStorage.getItem('currentUser');
    return storedUser ? JSON.parse(storedUser) : null;
  }

  /**
   * Loads invites for the logged-in user (FromId).
   */
  loadMyInvites(fromId: number): void {
    this.inviteService.getInvitesByFromId(fromId).subscribe({
      next: (invites) => {
        this.myInvites = invites;
      },
      error: (err) => {
        this.snackBar.open('Error loading invites: ' + err.message, 'Close', {
          duration: 3000,
        });
        console.error(err);
      },
    });
  }

  /**
   * Submits the form to send an invite.
   */
  sendInvite(): void {
    if (!this.currentUser) {
      this.snackBar.open('No current user available!', 'Close', { duration: 3000 });
      return;
    }

    if (this.inviteForm.valid) {
      const inviteData: Invite = {
        fromId: this.currentUser.id,
        toName: this.inviteForm.value.toName,
        toEmail: this.inviteForm.value.toEmail,
        website: '' // Will get overwritten in service with window.location.origin
      };

      this.inviteService.sendInvite(inviteData).subscribe({
        next: (res) => {
          this.snackBar.open(res.message, 'Close', { duration: 3000 });
          this.loadMyInvites(this.currentUser!.id); // Reload invites
          this.inviteForm.reset();
        },
        error: (err) => {
          this.snackBar.open('Error sending invite: ' + err.message, 'Close', {
            duration: 3000,
          });
          console.error(err);
        },
      });
    } else {
      this.snackBar.open('Please fill out the form correctly.', 'Close', {
        duration: 3000,
      });
    }
  }
}

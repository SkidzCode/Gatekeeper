import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UserService } from '../../services/user/user.service';
import { User } from '../../models/user.model';

// Define the User interface

@Component({
  selector: 'app-user-profile',
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.scss'],
  standalone: false
})

export class UserProfileComponent implements OnInit {
  user: User | null = null;
  profileForm: FormGroup;

  constructor(private fb: FormBuilder, private snackBar: MatSnackBar, private userService: UserService) {
    // Initialize the form with form controls and validators
    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      username: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^[0-9\-+()\s]{7,20}$/)]]
      // Add other form controls as necessary
    });
  }

  ngOnInit(): void {
    this.user = this.getUser();
    if (this.user) {
      // Populate the form with user data
      this.profileForm.patchValue({
        firstName: this.user.firstName,
        lastName: this.user.lastName,
        username: this.user.username,
        email: this.user.email,
        phone: this.user.phone
        // Patch other fields as necessary
      });
    }
  }

  /**
   * Retrieves the current user from localStorage.
   * @returns The User object if found, otherwise null.
   */
  private getUser(): User | null {
    const userJson = localStorage.getItem('currentUser');
    return userJson ? JSON.parse(userJson) : null;
  }

  /**
   * Saves the updated user information.
   * Updates localStorage and provides user feedback.
   */
  saveUser(): void {
    if (this.profileForm.valid && this.user) {
      const updatedUser: User = {
        ...this.user,
        ...this.profileForm.value
      };

      this.userService.updateUser(updatedUser).subscribe({
        next: (res) => {
          // The user was successfully updated on the server
          localStorage.setItem('currentUser', JSON.stringify(updatedUser));
          this.snackBar.open('Profile updated successfully!', 'Close', { duration: 3000 });
          console.log('User information updated:', updatedUser);
        },
        error: (err) => {
          // Handle any error that might come from the API
          console.error('Error updating user:', err);
          this.snackBar.open('Failed to update profile. Please try again later.', 'Close', { duration: 3000 });
        }
      });
    } else {
      this.snackBar.open('Please fix the errors in the form.', 'Close', { duration: 3000 });
      console.error('Form is invalid or user data is missing.');
    }
  }

}

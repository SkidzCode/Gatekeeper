import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UserService } from '../../core/services/user/user.service'; // Corrected path
import { User } from '../../shared/models/user.model'; // Corrected path


@Component({
  selector: 'app-profile', // Changed selector
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
  standalone: false // Explicitly set to false as per original component
})
export class ProfileComponent implements OnInit { // Changed class name
  user: User | null = null;
  profileForm: FormGroup;
  selectedFile: File | null = null;
  imageError: string | null = null;
  profileImageUrl: string | null = null;

  constructor(private fb: FormBuilder, private snackBar: MatSnackBar, private userService: UserService) {
    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      username: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^[0-9\-+()\s]{7,20}$/)]]
    });
  }

  ngOnInit(): void {
    this.user = this.getUser();
    if (this.user) {
      this.profileForm.patchValue({
        firstName: this.user.firstName,
        lastName: this.user.lastName,
        username: this.user.username,
        email: this.user.email,
        phone: this.user.phone
      });
      this.refreshProfileImageUrl();
    }
  }
    /**
   * Retrieves the current user from localStorage.
   * @returns The User object if found, otherwise null.
   */
  public getUser(): User | null {
    const userJson = localStorage.getItem('currentUser');
    return userJson ? JSON.parse(userJson) : null;
  }

  onFileSelected(event: Event): void {
    const fileInput = event.target as HTMLInputElement;

    if (fileInput.files && fileInput.files.length > 0) {
      const file = fileInput.files[0];
      var isSet: boolean = false;
      if (this.validateImage(file)) {
        const reader = new FileReader();
        reader.onload = (e: any) => {
          const img = new Image();
          img.onload = () => {
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            const MAX_WIDTH = 200;
            const scaleSize = MAX_WIDTH / img.width;
            canvas.width = MAX_WIDTH;
            canvas.height = img.height * scaleSize;
            ctx?.drawImage(img, 0, 0, canvas.width, canvas.height);
            canvas.toBlob((blob) => {
              if (blob) {
                this.selectedFile = new File([blob], file.name, { type: file.type });
                this.imageError = null;
              }
            }, file.type);

          };
          img.src = e.target.result;
        };
        reader.readAsDataURL(file);
      }
    }
  }

  private validateImage(file: File): boolean {
    const validTypes = ['image/jpeg', 'image/png'];
    const maxSizeInBytes = 5 * 1024 * 1024; // 5MB

    if (!validTypes.includes(file.type)) {
      this.imageError = 'Only JPEG and PNG files are allowed.';
      this.snackBar.open(this.imageError, 'Close', { duration: 3000 });
      return false;
    }

    if (file.size > maxSizeInBytes) {
      this.imageError = 'File size should not exceed 5MB.';
      this.snackBar.open(this.imageError, 'Close', { duration: 3000 });
      return false;
    }

    return true;
  }


    /**
   * Saves the updated user information.
   * Updates localStorage and provides user feedback.
   */
    saveUser(): void {
    if (this.profileForm.valid && this.user) {
      const formData = new FormData();

      // Append form fields
      Object.keys(this.profileForm.controls).forEach(key => {
        formData.append(key, this.profileForm.get(key)?.value);
      });

      formData.append('id', this.user.id.toString());

      // Append the file if selected
      if (this.selectedFile) {
        formData.append('ProfilePicture', this.selectedFile);
      }

      this.userService.updateUserWithImage(formData).subscribe({
        next: (res: any) => { // Added type for res
          localStorage.setItem('currentUser', JSON.stringify(res.user));
          this.snackBar.open('Profile updated successfully!', 'Close', { duration: 3000 });
          this.refreshProfileImageUrl();
        },
        error: (err: any) => { // Added type for err
          console.error('Error updating user:', err);
          this.snackBar.open('Failed to update profile. Please try again later.', 'Close', { duration: 3000 });
        }
      });
    } else {
      this.snackBar.open('Please fix the errors in the form.', 'Close', { duration: 3000 });
      console.error('Form is invalid or user data is missing.');
    }
  }

  private refreshProfileImageUrl(): void {
    if (this.user) {
      this.profileImageUrl = `/api/User/ProfilePicture/${this.user.id}?timestamp=${new Date().getTime()}`;
    }
  }
}

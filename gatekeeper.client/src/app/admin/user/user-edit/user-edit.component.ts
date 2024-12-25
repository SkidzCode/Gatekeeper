import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService, User } from '../../../services/user.service';

@Component({
  selector: 'app-user-edit',
  templateUrl: './user-edit.component.html',
  styleUrls: ['./user-edit.component.scss'],
  standalone: false,
})
export class UserEditComponent implements OnInit {
  userId!: number;
  user: User = {
    id: 0,
    firstName: '',
    lastName: '',
    email: '',
    username: '',
    phone: '',
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: UserService
  ) { }

  ngOnInit(): void {
    // Parse userId from the route: /admin/users/edit/:id
    this.route.params.subscribe((params) => {
      this.userId = +params['id'];
      this.loadUser(this.userId);
    });
  }

  loadUser(id: number): void {
    this.userService.getUserById(id).subscribe({
      next: (data) => {
        this.user = data;
      },
      error: (err) => console.error('Error loading user', err),
    });
  }

  saveUser(): void {
    this.userService.updateUser(this.user).subscribe({
      next: (res) => {
        alert('User updated successfully!');
        // Navigate back to the user list
        this.router.navigate(['/admin', 'users']);
      },
      error: (err) => {
        console.error('Error updating user', err);
        alert('Failed to update user');
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/admin', 'users']);
  }
}

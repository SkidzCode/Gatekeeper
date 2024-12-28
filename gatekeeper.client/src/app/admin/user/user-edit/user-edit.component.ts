import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService } from '../../../services/user/user.service';
import { User } from '../../../models/user.model';
import { Role } from '../../../models/role.model';

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
    roles: [],
    isActive: false
  };

  // This will hold all possible roles from the backend
  userRoles: Role[] = []; // e.g. [{ id: 1, name: 'ADMIN' }, { id: 2, name: 'USER' }]

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
    this.userService.getUserByIdEdit(id).subscribe({
      next: (data) => {
        // data.user is your User object
        this.user = data.user;
        // data.roles is your list of all possible roles
        this.userRoles = data.roles;
      },
      error: (err) => console.error('Error loading user', err),
    });
  }

  saveUser(): void {
    // user.roles has the updated list of role names
    this.userService.updateUser(this.user).subscribe({
      next: () => {
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

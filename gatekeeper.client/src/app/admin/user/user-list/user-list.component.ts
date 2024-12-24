import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { UserService, User } from '../../../services/user.service';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.css'],
  standalone: false
})
export class UserListComponent implements OnInit {
  users: User[] = [];

  constructor(
    private userService: UserService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.userService.getUsers().subscribe({
      next: (data) => (this.users = data),
      error: (err) => console.error(err),
    });
  }

  editUser(user: User): void {
    // Navigate to the user-edit route
    // e.g. /admin/users/edit/5
    this.router.navigate(['/admin', 'users', 'edit', user.id]);
  }
}

import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService } from '../../../core/services/user/user.service';
import { User } from '../../../shared/models/user.model';
import { Role } from '../../../shared/models/role.model';
import { SessionService } from '../../../core/services/site/session.service';
import { SessionModel } from '../../../shared/models/session.model';

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
    isActive: false,
  };
  profileImageUrl: string | null = null;
  userRoles: Role[] = [];
  sessions: SessionModel[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: UserService,
    private sessionService: SessionService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      this.userId = +params['id'];
      this.loadUser(this.userId);
    });
  }

  loadUser(id: number): void {
    this.userService.getUserByIdEdit(id).subscribe({
      next: (data) => {
        this.user = data.user;
        this.userRoles = data.roles;
        this.refreshProfileImageUrl();
        this.loadSessions();
      },
      error: (err) => console.error('Error loading user', err),
    });
  }

  loadSessions(): void {
    this.sessionService.getActiveSessionsUser(this.userId).subscribe(
      (sessions) => {
        this.sessions = sessions;
      },
      (error) => {
        console.error('Error loading sessions:', error);
      }
    );
  }

  logoutFromSession(sessionId: string): void {
    this.sessionService.revokeSession(sessionId).subscribe(
      () => {
        console.log('Logged out from session:', sessionId);
        this.loadSessions();
      },
      (error) => {
        console.error('Error logging out from session:', error);
      }
    );
  }

  saveUser(): void {
    this.userService.updateUser(this.user).subscribe({
      next: () => {
        alert('User updated successfully!');
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

  private refreshProfileImageUrl(): void {
    if (this.user) {
      this.profileImageUrl = `/api/User/ProfilePicture/${this.user.id}?timestamp=${new Date().getTime()}`;
    }
  }
}

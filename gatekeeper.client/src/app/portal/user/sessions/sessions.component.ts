import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../../core/services/user/auth.service';
import { SessionService } from '../../../core/services/site/session.service';
import { SessionModel } from '../../../shared/models/session.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-sessions',
  templateUrl: './sessions.component.html',
  styleUrls: ['./sessions.component.scss'],
  standalone:false
})
export class SessionsComponent implements OnInit {
  sessions: SessionModel[] = [];
  currentSessionId: string | null = null;

  constructor(private router: Router, private authService: AuthService, private sessionService: SessionService) {}

  ngOnInit(): void {
    this.loadSessions();
    this.currentSessionId = this.authService.getSessionId();
  }

  loadSessions(): void {
    this.sessionService.getActiveSessions().subscribe(
      (sessions) => {
        this.sessions = sessions;
      },
      (error) => {
        console.error('Error loading sessions:', error);
      }
    );
  }

  logoutFromSession(sessionId: string): void {
    if (sessionId == this.currentSessionId) {
      this.authService.logoutCurrentSession().subscribe(message => {
        console.log(message);
        this.router.navigate(['/']);
      });
    }
    else {
      this.authService.logoutFromDevice(sessionId).subscribe(
        (response) => {
          console.log('Logged out from session:', sessionId);
          this.loadSessions(); // Refresh the sessions list
        },
        (error) => {
          console.error('Error logging out from session:', error);
        }
      );
    }
  }
}

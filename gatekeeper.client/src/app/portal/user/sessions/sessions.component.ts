import { Component, OnInit } from '@angular/core';
import { SessionService } from '../../../core/services/site/session.service';
import { SessionModel } from '../../../shared/models/session.model';

@Component({
  selector: 'app-sessions',
  templateUrl: './sessions.component.html',
  styleUrls: ['./sessions.component.scss'],
  standalone: false,
})
export class SessionsComponent implements OnInit {
  sessions: SessionModel[] = [];

  constructor(private sessionService: SessionService) { }

  ngOnInit(): void {
    const userId = this.getUserId();
    if (userId) {
      this.sessionService.getActiveSessions().subscribe({
        next: (sessions) => {
          this.sessions = sessions;
        },
        error: (err) => {
          console.error('Error fetching sessions:', err);
        }
      });
    }
  }

  private getUserId(): number | null {
    const userJson = localStorage.getItem('currentUser');
    const user = userJson ? JSON.parse(userJson) : null;
    return user ? user.id : null;
  }
}

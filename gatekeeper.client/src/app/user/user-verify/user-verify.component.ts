import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';


@Component({
  selector: 'app-user-verify',
  standalone: false,
  
  templateUrl: './user-verify.component.html',
  styleUrl: './user-verify.component.scss'
})
export class UserVerifyComponent implements OnInit {
  token: string = '';
  successMessage: string = '';
  constructor(private router: Router, private route: ActivatedRoute, private authService: AuthService) { }

  ngOnInit(): void {
    // Retrieve the token from query parameters
    this.route.queryParams.subscribe(params => {
      this.token = params['token'] ? decodeURIComponent(params['token']) : '';
      this.authService.verifyUser(this.token).subscribe({
        next: (res) => {
          this.successMessage = 'Email validated!';
          // Navigate to login after a short delay
          setTimeout(() => {
            this.router.navigate(['/login']);
          }, 3000);
        },
        error: (err) => {
          this.successMessage = '';
        }
      });
    });
  }
}

import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-dash-board',
  standalone: false,
  
  templateUrl: './dash-board.component.html',
  styleUrl: './dash-board.component.css'
})
export class DashBoardComponent {
  data: any;

  constructor(private http: HttpClient) { }

  testApi(): void {
    // Call any protected endpoint that requires a valid access token
    this.http.get('/api/WeatherForecast').subscribe({
      next: (response) => {
        this.data = response;
        console.log('API call successful:', response);
      },
      error: (error) => {
        console.error('API call failed:', error);
      },
    });
  }
}

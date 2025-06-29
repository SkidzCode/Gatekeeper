import { Component } from '@angular/core';

@Component({
  selector: 'app-plugin-load-error',
  template: `
    <div class="error-container">
      <h2>Plugin Error</h2>
      <p>There was an issue loading this part of the application. Please try again later or contact support if the problem persists.</p>
    </div>
  `,
  styles: [`
    .error-container {
      padding: 20px;
      border: 1px solid #ffcccb; /* Light red border */
      background-color: #fff5f5; /* Lighter red background */
      color: #d8000c; /* Dark red text */
      border-radius: 5px;
      text-align: center;
    }
    .error-container h2 {
      margin-top: 0;
      color: #d8000c;
    }
  `],
  standalone: true,
})
export class PluginLoadErrorComponent {}

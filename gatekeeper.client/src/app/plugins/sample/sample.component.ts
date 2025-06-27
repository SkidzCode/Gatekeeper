import { Component } from '@angular/core';

import { CommonModule } from '@angular/common'; // Good practice for standalone components

@Component({
  selector: 'app-sample',
  standalone: true,
  imports: [CommonModule], // Import CommonModule if using ngIf, ngFor, etc.
  templateUrl: './sample.component.html',
})
export class SampleComponent {
  message = 'Hello from Sample Plugin!';
}

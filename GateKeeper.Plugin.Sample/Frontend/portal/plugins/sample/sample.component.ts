import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-sample',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sample.component.html',
})
export class SampleComponent {
  message = 'Hello from Sample Plugin!';
}

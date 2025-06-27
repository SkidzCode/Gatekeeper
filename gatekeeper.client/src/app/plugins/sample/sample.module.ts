import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SampleRoutingModule } from './sample-routing.module';
import { SampleComponent } from './sample.component';

@NgModule({
  declarations: [], // SampleComponent is now standalone
  imports: [
    CommonModule,
    SampleRoutingModule,
    SampleComponent // Import standalone components
  ]
})
export class SampleModule { }

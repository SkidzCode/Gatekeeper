import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SampleAdminComponent } from './sample-admin.component';
import { SampleAdminRoutingModule } from './sample-admin-routing.module';

@NgModule({
  imports: [
    CommonModule,
    SampleAdminRoutingModule,
    SampleAdminComponent
  ]
})
export class SampleAdminModule { }

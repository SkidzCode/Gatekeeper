import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProfileAdminComponent } from './profile-admin.component';
import { ProfileAdminRoutingModule } from './profile-admin-routing.module';

@NgModule({
  imports: [
    CommonModule,
    ProfileAdminRoutingModule,
    ProfileAdminComponent
  ]
})
export class ProfileAdminModule { }

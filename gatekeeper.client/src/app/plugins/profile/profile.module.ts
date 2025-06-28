import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router'; // Import RouterModule
import { ProfileComponent } from './profile.component';
import { ProfileRoutingModule } from './profile-routing.module';

@NgModule({
  declarations: [
    ProfileComponent
  ],
  imports: [
    CommonModule,
    ProfileRoutingModule, // Add the routing module here
    RouterModule // Also import RouterModule directly if routes are defined here or re-exported by ProfileRoutingModule
  ],
  // exports: [ // No need to export components if they are only used for routing within this module
  //   ProfileComponent
  // ]
})
export class ProfileModule { }

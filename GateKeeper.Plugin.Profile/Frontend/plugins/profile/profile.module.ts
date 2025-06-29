import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';

import { ProfileComponent } from './profile.component';
import { ProfileRoutingModule } from './profile-routing.module';



@NgModule({
  declarations: [
    // ProfileComponent // Removed as it's standalone
  ],
  imports: [
    CommonModule,
    RouterModule, // RouterModule is needed for routerLink, router-outlet etc. within the module's components
    ProfileRoutingModule
  ]
  // No exports needed if ProfileComponent is only routed within this lazy-loaded module
})
export class ProfileModule { }
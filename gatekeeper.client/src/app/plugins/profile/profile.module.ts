import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router'; // Import RouterModule
import { ProfileComponent } from './profile.component';
import { ProfileRoutingModule } from './profile-routing.module';

import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms'; // Import ReactiveFormsModule

import { ProfileComponent } from './profile.component';
import { ProfileRoutingModule } from './profile-routing.module';

// Angular Material Modules
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBarModule } from '@angular/material/snack-bar';


@NgModule({
  declarations: [
    ProfileComponent // Declare the non-standalone ProfileComponent
  ],
  imports: [
    CommonModule,
    ProfileRoutingModule,
    RouterModule,
    ReactiveFormsModule, // Add ReactiveFormsModule
    // Add Angular Material Modules
    MatCardModule,
    MatIconModule,
    MatDividerModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule
  ],
  // No exports needed if ProfileComponent is only routed within this lazy-loaded module
})
export class ProfileModule { }

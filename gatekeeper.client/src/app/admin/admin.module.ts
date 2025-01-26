import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';  // or ReactiveFormsModule
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatOptionModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatListModule } from '@angular/material/list';
import { MatNativeDateModule } from '@angular/material/core'; // <-- Import here
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatTabsModule } from '@angular/material/tabs'; 


import { AdminRoutingModule } from './admin-routing.module';
import { UserListComponent } from './user/user-list/user-list.component';
import { UserEditComponent } from './user/user-edit/user-edit.component';
import { NotificationComponent } from './notification/notification/notification.component';
import { NotificationPreviewDialogComponent } from './notification/notification-preview-dialog/notification-preview-dialog.component';
import { NotificationTemplatesComponent } from './notification/notification-templates/notification-templates.component';
import { TemplatePreviewIframeComponent } from './notification/template-preview-iframe/template-preview-iframe.component';
import { AdminLogsBrowserComponent } from './history/admin-logs-browser/admin-logs-browser.component';
import { AdminSettingsListComponent } from './settings/admin-settings-list/admin-settings-list.component';
import { AdminSettingsEditComponent } from './settings/admin-settings-edit/admin-settings-edit.component';
import { HomeComponent } from './home/home/home.component';

@NgModule({
  declarations: [
    UserListComponent,
    UserEditComponent,
    NotificationComponent,
    NotificationPreviewDialogComponent,
    NotificationTemplatesComponent,
    TemplatePreviewIframeComponent,
    AdminLogsBrowserComponent,
    AdminSettingsListComponent,
    AdminSettingsEditComponent,
    HomeComponent,
    
  ],
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    AdminRoutingModule,
    MatPaginatorModule,
    MatSortModule,
    MatOptionModule,
    MatSelectModule,
    MatCardModule,
    MatDividerModule,
    MatCheckboxModule,
    ReactiveFormsModule,
    MatDatepickerModule,
    MatListModule,
    MatNativeDateModule,
    MatToolbarModule,
    MatIconModule,
    MatProgressSpinner,
    MatRadioModule,
    MatTabsModule
  ],
})
export class AdminModule { }

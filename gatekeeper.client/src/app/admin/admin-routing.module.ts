import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Layout
import { AdminLayoutComponent } from './layout/admin-layout/admin-layout.component';

// Components
import { HomeComponent } from './home/home/home.component'; 
import { UserListComponent } from './user/user-list/user-list.component';
import { UserEditComponent } from './user/user-edit/user-edit.component';
import { UserLoginComponent } from '../user/user-login/user-login.component';
import { NotificationComponent } from './notification/notification/notification.component';
import { NotificationTemplatesComponent } from './notification/notification-templates/notification-templates.component';
import { AdminLogsBrowserComponent } from './history/admin-logs-browser/admin-logs-browser.component'
import { ResourcesEditorComponent } from '../resources/resources-editor/resources-editor.component';
import { AdminSettingsListComponent } from './settings/admin-settings-list/admin-settings-list.component';
import { AdminSettingsEditComponent } from './settings/admin-settings-edit/admin-settings-edit.component';

// Guard
import { AdminGuard } from '../services/guard/admin-guard.service';

const routes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent, // <--- Admin layout wrapper
    children: [
      { path: '', component: HomeComponent, canActivate: [AdminGuard] },
      { path: 'users', component: UserListComponent, canActivate: [AdminGuard] },
      { path: 'users/edit/:id', component: UserEditComponent, canActivate: [AdminGuard] },
      { path: 'notifications/send', component: NotificationComponent, canActivate: [AdminGuard] },
      { path: 'notifications/templates', component: NotificationTemplatesComponent, canActivate: [AdminGuard] },
      { path: 'logs', component: AdminLogsBrowserComponent, canActivate: [AdminGuard] },
      { path: 'login', component: UserLoginComponent, canActivate: [AdminGuard] },
      { path: 'resources', component: ResourcesEditorComponent, canActivate: [AdminGuard] },
      { path: 'settings', component: AdminSettingsListComponent, canActivate: [AdminGuard] },      // e.g., /admin/settings
      { path: 'settings/new', component: AdminSettingsEditComponent, canActivate: [AdminGuard] },   // e.g., /admin/settings/new
      { path: 'settings/edit/:id', component: AdminSettingsEditComponent, canActivate: [AdminGuard] }, // e.g., /admin/settings/edit/123
      // ... more admin routes here
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AdminRoutingModule { }

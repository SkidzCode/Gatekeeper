import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Layout
import { AdminLayoutComponent } from '../site/layout/admin-layout/admin-layout.component';

// Components
import { UserListComponent } from './user/user-list/user-list.component';
import { UserEditComponent } from './user/user-edit/user-edit.component';
import { UserLoginComponent } from '../user/user-login/user-login.component';
import { NotificationComponent } from './notification/notification/notification.component';
import { NotificationTemplatesComponent } from './notification/notification-templates/notification-templates.component';

// Guard
import { AdminGuard } from '../services/guard/admin-guard.service';

const routes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent, // <--- Admin layout wrapper
    children: [
      { path: 'users', component: UserListComponent, canActivate: [AdminGuard] },
      { path: 'users/edit/:id', component: UserEditComponent, canActivate: [AdminGuard] },
      { path: 'notifications/send', component: NotificationComponent, canActivate: [AdminGuard] },
      { path: 'notifications/templates', component: NotificationTemplatesComponent },
      { path: 'login', component: UserLoginComponent }
      // ... more admin routes here
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AdminRoutingModule { }

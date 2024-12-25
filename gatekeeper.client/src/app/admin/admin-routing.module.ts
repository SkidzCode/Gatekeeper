import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Layout
import { AdminLayoutComponent } from '../layout/admin-layout/admin-layout.component';

// Components
import { UserListComponent } from './user/user-list/user-list.component';
import { UserEditComponent } from './user/user-edit/user-edit.component';

const routes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent, // <--- Admin layout wrapper
    children: [
      { path: 'users', component: UserListComponent },
      { path: 'users/edit/:id', component: UserEditComponent },
      // ... more admin routes here
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AdminRoutingModule { }

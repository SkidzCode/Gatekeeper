import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SampleAdminComponent } from './sample-admin.component';

const routes: Routes = [
  {
    path: '',
    component: SampleAdminComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SampleAdminRoutingModule { }

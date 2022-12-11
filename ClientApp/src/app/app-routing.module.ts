import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CallbackComponent } from './callback/callback.component';
import { MainComponent } from './main/main.component';

const routes: Routes = [
  {
    path: 'callback',
    component: CallbackComponent,
  },
  {
    path: '',
    component: MainComponent
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }

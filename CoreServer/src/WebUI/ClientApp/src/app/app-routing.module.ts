import {NgModule} from '@angular/core';
import {Routes, RouterModule} from '@angular/router';
import {AuthorizeGuard} from './services/auth/authorize.guard';
import {LayoutComponent} from "./components/layout/layout.component";
import {ChatComponent} from "./components/chat/chat.component";
import {LoginComponent} from "./pages/login/login.component";
import {RegisterComponent} from "./pages/register/register.component";

export const routes: Routes = [
  {
    path: '', component: LayoutComponent, canActivate: [AuthorizeGuard], children: [
      {path: 'chat', component: ChatComponent},
    ]
  },
  {path:'login',component: LoginComponent},
  {path:'register',component: RegisterComponent},
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {
}

import {NgModule} from '@angular/core';
import {Routes, RouterModule} from '@angular/router';
import {AuthorizeGuard} from './services/auth/authorize.guard';
import {LayoutComponent} from "./components/layout/layout.component";
import {LoginComponent} from "./pages/login/login.component";
import {RegisterComponent} from "./pages/register/register.component";
import {ChatPageComponent} from "./pages/chat/chat-page.component";
import {SessionListPageComponent} from "./pages/session-list/session-list.component";
import {CallComponent} from "./pages/call/call.component";

export const routes: Routes = [
  {
    path: '', component: LayoutComponent, canActivate: [AuthorizeGuard], children: [
      {path: 'chat', component: ChatPageComponent},
      {path:'sessions',component: SessionListPageComponent},
      {path:'call',component: CallComponent},
      {path:'call/:id',component: CallComponent}
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

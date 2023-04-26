import {BrowserModule} from '@angular/platform-browser';
import {InjectionToken, NgModule} from '@angular/core';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import {HttpClientModule, HTTP_INTERCEPTORS} from '@angular/common/http';

import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';

import {AuthorizeInterceptor} from 'src/app/services/auth/authorize.interceptor';
import {BrowserAnimationsModule} from '@angular/platform-browser/animations';
import {LayoutComponent} from "./components/layout/layout.component";
import {SidebarComponent} from "./components/sidebar/sidebar.component";
import {ChatComponent} from "./components/chat/chat.component";
import {MessageComponent} from "./components/chat/message/message.component";
import {AvatarComponent} from "./components/avatar/avatar.component";
import {HeaderComponent} from "./components/header/header.component";
import {LoginComponent} from './pages/login/login.component';
import {RegisterComponent} from './pages/register/register.component';
import {NotificationComponent} from './components/notification/notification.component';
import {ErrorInterceptor} from "./services/error-interceptor.service";
import {NgxPopperjsModule} from "ngx-popperjs";
import {SecurePipe} from './services/secure.pipe';
import {UserPipe} from './services/user/user.pipe';
import {ChatPageComponent} from "./pages/chat/chat-page.component";
import {environment} from '../environments/environment';
import {API_BASE_URL} from "./web-api-client";
import { UserSelectComponent } from './components/user-select/user-select.component';

function baseUrlFactory() {
  if(environment.production){
    const href = document.getElementsByTagName('base')[0].href;
    // get the hostname with this regex: ^((?>\w+:\/\/)?[^\/]+)
    return href.match(/^((\w+:\/\/)?[^\/]+)/)[0];

  }
  const url = document.getElementsByTagName('base')[0].href;
  const port = url.split(':')[2];
  if (port) {
    return url.replace(port, environment.backendPort.toString());
  }
  return `${url}:${environment.backendPort}`;
}

@NgModule({
  declarations: [
    AppComponent,
    LayoutComponent,
    SidebarComponent,
    HeaderComponent,
    ChatComponent,
    ChatPageComponent,
    MessageComponent,
    AvatarComponent,
    LoginComponent,
    RegisterComponent,
    NotificationComponent,
    SecurePipe,
    UserPipe,
    UserSelectComponent,
  ],
    imports: [
        BrowserModule.withServerTransition({appId: 'ng-cli-universal'}),
        HttpClientModule,
        FormsModule,
        AppRoutingModule,
        NgxPopperjsModule.forRoot({applyArrowClass: 'invisible'}),
        BrowserAnimationsModule,
        ReactiveFormsModule
    ],
  providers: [
    {provide: API_BASE_URL, useFactory: baseUrlFactory, deps: []},
    {provide: HTTP_INTERCEPTORS, useClass: AuthorizeInterceptor, multi: true},
    {provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true}
  ],
  bootstrap: [AppComponent]
})
export class AppModule {
}

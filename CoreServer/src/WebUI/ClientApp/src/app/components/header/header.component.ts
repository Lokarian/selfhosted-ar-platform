import {Component} from '@angular/core';
import {NgxPopperjsPlacements} from "ngx-popperjs";
import {CurrentUserService} from "../../services/user/current-user.service";
import {AuthorizeService} from "../../services/auth/authorize.service";
import {ArClient, ArUserRole, JoinArSessionCommand} from "../../web-api-client";

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  public ngxPopperjsPlacements = NgxPopperjsPlacements;
  constructor(private currentUserService:CurrentUserService,private authService:AuthorizeService,private arClient:ArClient) {

  }
  public get user$() {
    return this.currentUserService.user$;
  }

  signOut() {
    this.authService.removeAccessToken(true);
  }
  joinArSession() {
    this.arClient.joinArSession(new JoinArSessionCommand({arSessionId:"453fccb4-1ab0-47c2-9fe2-e3b33edb4e62",role:ArUserRole.Web})).subscribe()
  }
  joinArSession2() {
    this.arClient.joinArSession(new JoinArSessionCommand({arSessionId:"453fccb4-1ab0-47c2-9fe2-e3b33edb4e62",role:ArUserRole.Hololens})).subscribe()
  }
}

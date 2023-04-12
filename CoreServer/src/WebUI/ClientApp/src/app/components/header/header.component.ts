import {Component} from '@angular/core';
import {OnlineStatus, AppUser} from "../../models/appUser";
import {NgxPopperjsPlacements} from "ngx-popperjs";
import {CurrentUserService} from "../../services/current-user.service";
import {AuthorizeService} from "../../services/auth/authorize.service";

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  public ngxPopperjsPlacements = NgxPopperjsPlacements;
  constructor(private currentUserService:CurrentUserService,private authService:AuthorizeService) { }
  public get user$() {
    return this.currentUserService.user$;
  }

  signOut() {
    this.authService.removeAccessToken(true);
  }
}

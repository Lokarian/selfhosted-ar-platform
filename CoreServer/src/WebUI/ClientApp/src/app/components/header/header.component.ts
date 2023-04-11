import {Component} from '@angular/core';
import {OnlineStatus, AppUser} from "../../models/appUser";
import {NgxPopperjsPlacements} from "ngx-popperjs";
import {CurrentUserService} from "../../services/current-user.service";

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  public ngxPopperjsPlacements = NgxPopperjsPlacements;
  constructor(private currentUserService:CurrentUserService) { }
  public get user$() {
    return this.currentUserService.user$;
  }
}

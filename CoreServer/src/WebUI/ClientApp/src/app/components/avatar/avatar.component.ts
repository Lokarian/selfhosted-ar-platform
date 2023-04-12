import {Component, Input} from '@angular/core';
import {AppUser,OnlineStatus} from "../../web-api-client";

@Component({
  selector: 'app-avatar[user]',
  templateUrl: './avatar.component.html',
  styleUrls: ['./avatar.component.scss']
})
export class AvatarComponent {
  @Input() user: AppUser;
  @Input() size: number =4;
  @Input() showStatus: boolean = true;

  // give the enum to the template
  public OnlineStatus = OnlineStatus;

}

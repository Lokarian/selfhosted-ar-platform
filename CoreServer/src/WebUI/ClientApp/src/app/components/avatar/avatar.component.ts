import {Component, Input} from '@angular/core';
import {OnlineStatus, User} from "../../models/user";

@Component({
  selector: 'app-avatar[user]',
  templateUrl: './avatar.component.html',
  styleUrls: ['./avatar.component.scss']
})
export class AvatarComponent {
  @Input() user: User;
  @Input() size: number =4;
  @Input() showStatus: boolean = true;

  // give the enum to the template
  public OnlineStatus = OnlineStatus;

}

import {Component} from '@angular/core';
import {OnlineStatus, User} from "../../models/user";

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  public user:User = {
    id: 1,
    name: 'John Doe',
    image: 'https://picsum.photos/200',
    status: OnlineStatus.Online
  }
}

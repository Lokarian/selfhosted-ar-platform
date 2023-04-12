import {Component} from '@angular/core';
import {ChatMessage} from "../../models/chat";
import {OnlineStatus} from "../../models/appUser";
import {AppUser} from "../../web-api-client";

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent {
  public messages: ChatMessage[] = [
    {
      message: 'Hi, I\'m fine, thanks!',
      sender: AppUser.fromJS({
        //id as random guid
        id:"a5c89bcf-9cc8-4ff1-8827-b865fd5da04b",
        userName: 'Jane Doe',
        onlineStatus: OnlineStatus.Online
      }),
      timestamp: 1554090956000
    },

  ];

}

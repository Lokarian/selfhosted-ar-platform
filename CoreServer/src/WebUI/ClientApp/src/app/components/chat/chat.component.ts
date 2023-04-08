import {Component} from '@angular/core';
import {ChatMessage} from "../../models/chat";
import {OnlineStatus} from "../../models/user";

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent {
  public messages: ChatMessage[] = [
    {
      message: 'Hi, I\'m fine, thanks!',
      sender: {
        id: 2,
        name: 'Jane Doe',
        image: 'https://picsum.photos/200',
        status: OnlineStatus.Online
      },
      timestamp: 1554090956000
    },    {
      message: 'Hello, how are you?',
      sender: {
        id: 1,
        name: 'John Doe',
        image: 'https://picsum.photos/200',
        status: OnlineStatus.Online
      },
      timestamp: 1554090856000
    }

  ];

}

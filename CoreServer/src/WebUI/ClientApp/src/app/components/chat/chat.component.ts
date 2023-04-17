import {Component} from '@angular/core';
import {AppUserDto, ChatMessageDto} from "../../web-api-client";

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent {
  public messages: ChatMessageDto[] = [
    ChatMessageDto.fromJS(    {
      text: 'Hi, I\'m fine, thanks!',
      senderId:"a5c89bcf-9cc8-4ff1-8827-b865fd5da04b",
      sentAt: new Date()
    }),

  ];

}

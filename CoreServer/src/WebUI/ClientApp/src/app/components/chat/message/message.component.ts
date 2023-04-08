import {Component, Input} from '@angular/core';
import {ChatMessage} from "../../../models/chat";


@Component({
  selector: 'app-chat-message[message]',
  templateUrl: './message.component.html',
  styleUrls: ['./message.component.scss']
})
export class MessageComponent {
  @Input() message: ChatMessage;

  public get isOwnMessage(): boolean {
    return this.message.sender.id === 2;//todo: get current user id
  }
}

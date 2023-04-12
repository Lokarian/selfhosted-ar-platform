import {Component, Input} from '@angular/core';
import {ChatMessage} from "../../../models/chat";
import {CurrentUserService} from "../../../services/current-user.service";


@Component({
  selector: 'app-chat-message[message]',
  templateUrl: './message.component.html',
  styleUrls: ['./message.component.scss']
})
export class MessageComponent {
  @Input() message: ChatMessage;
  constructor(private currentUserService: CurrentUserService) {
  }
  public get isOwnMessage(): boolean {
    return this.message.sender.id === this.currentUserService.user.id;
  }
}

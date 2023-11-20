import {Component, Input} from '@angular/core';
import {CurrentUserService} from "../../../services/user/current-user.service";
import {ChatMessageDto} from "../../../web-api-client";


@Component({
  selector: 'app-chat-message[message]',
  templateUrl: './message.component.html',
  styleUrls: ['./message.component.scss']
})
export class MessageComponent {
  @Input() message: ChatMessageDto;
  constructor(private currentUserService: CurrentUserService) {
  }
  public get isOwnMessage(): boolean {
    return this.message.senderId === this.currentUserService.user.id;
  }
}

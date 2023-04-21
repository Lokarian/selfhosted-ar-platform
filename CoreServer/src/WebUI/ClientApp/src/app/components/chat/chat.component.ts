import {Component, Input, OnChanges, OnInit} from '@angular/core';
import {ChatMessageDto, ChatSessionDto} from "../../web-api-client";
import {ChatService} from "../../services/chat.service";
import {Observable} from "rxjs";

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnChanges, OnInit {
  @Input() public session: ChatSessionDto;
  public messages$: Observable<ChatMessageDto[]>;
  private gettingMoreMessages = false;

  constructor(private chatService: ChatService) {

  }

  ngOnInit(): void {
    this.gettingMoreMessages = true;
    this.chatService.loadMoreMessages(this.session.id).then(() => {
      this.gettingMoreMessages = false;
    });
  }

  ngOnChanges(): void {
    this.messages$ = this.chatService.getChatMessages$(this.session.id);
  }

  onScroll(event: any) {
    console.log(event.target.scrollTop, event.target.scrollHeight, event.target.clientHeight);
    if (event.target.scrollHeight + event.target.scrollTop - event.target.clientHeight < 100) {
      if (!this.gettingMoreMessages) {
        this.gettingMoreMessages = true;
        this.chatService.loadMoreMessages(this.session.id).then(() => {
          this.gettingMoreMessages = false;
        });
      }
    }
  }
}

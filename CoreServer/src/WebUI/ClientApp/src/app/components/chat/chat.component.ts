import {Component, ElementRef, Input, OnChanges, OnInit, ViewChild} from '@angular/core';
import {ChatClient, ChatMessageDto, ChatSessionDto, SendMessageToChatSessionCommand} from "../../web-api-client";
import {ChatFacade} from "../../services/chat-facade.service";
import {Observable} from "rxjs";

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnChanges, OnInit {
  @Input() public session: ChatSessionDto;
  @ViewChild('textArea') private textArea: ElementRef;
  public messages$: Observable<ChatMessageDto[]>;
  private gettingMoreMessages = false;

  constructor(private chatService: ChatFacade, private chatClient: ChatClient) {

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
    if (event.target.scrollHeight + event.target.scrollTop - event.target.clientHeight < 100) {
      if (!this.gettingMoreMessages) {
        this.gettingMoreMessages = true;
        this.chatService.loadMoreMessages(this.session.id).then(() => {
          this.gettingMoreMessages = false;
        });
      }
    }
  }

  sendMessage() {
    const message = this.textArea.nativeElement.value;
    if (!message || message.trim().length === 0) {
      return;
    }
    this.chatClient.sendMessageToChatSession(new SendMessageToChatSessionCommand({
      sessionId: this.session.id,
      text: message
    })).subscribe(()=> {
      this.textArea.nativeElement.value = '';
    });
  }

}

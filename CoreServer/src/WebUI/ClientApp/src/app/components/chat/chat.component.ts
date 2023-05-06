import {
  ApplicationRef,
  ChangeDetectorRef,
  Component,
  ElementRef,
  Input, NgZone,
  OnChanges,
  OnInit,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import {
  ChatClient,
  ChatMessageDto,
  ChatSessionDto, SendMessageToChatCommand,
} from "../../web-api-client";
import {ChatFacade} from "../../services/chat-facade.service";
import {BehaviorSubject, Observable, of, ReplaySubject, switchMap} from "rxjs";
import {tap} from "rxjs/operators";

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit {

  private _session: ChatSessionDto;
  @Input() set session(value: ChatSessionDto) {
    this._session = value;
    this.loadMoreMessages();
  }
  get session(): ChatSessionDto {
    return this._session;
  }

  @ViewChild('textArea') private textArea: ElementRef;

  public messages$: Observable<ChatMessageDto[]>;
  private gettingMoreMessages = false;
  private gotAllMessages = false;

  constructor(private chatFacade: ChatFacade, private chatClient: ChatClient,private appRef:ApplicationRef ) {

  }

  ngOnInit(): void {
    this.chatFacade.updateLastRead(this.session.baseSessionId);
  }

  loadMoreMessages() {
    this.gettingMoreMessages = true;
    this.chatFacade.loadMoreMessages(this.session.baseSessionId).then((amount) => {
      setTimeout(() => this.gettingMoreMessages = false, 100);
      if (amount === 0) {
        this.gotAllMessages = true;
      }
    });
  }

  onScroll(event: any) {
    if (event.target.scrollHeight + event.target.scrollTop - event.target.clientHeight < 100) {
      if (!this.gettingMoreMessages && !this.gotAllMessages) {
        this.loadMoreMessages();
      }
    }
  }

  sendMessage(event?: any) {
    //event is from (keyup.enter) on textarea, if shift is also pressed, don't send
    if (event) {
      if (event.shiftKey) {
        return;
      }
      event.preventDefault();
    }
    const message = this.textArea.nativeElement.value;
    if (!message || message.trim().length === 0) {
      return;
    }
    this.chatClient.sendMessageToChatSession(new SendMessageToChatCommand({
      sessionId: this.session.baseSessionId,
      text: message
    })).subscribe(() => {
      this.textArea.nativeElement.value = '';
    });
  }

  handleKeydown(e: any) {
    if (!e.shiftKey) {
      e.preventDefault();
    }
  }
}

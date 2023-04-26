import {Component, ElementRef, Input, OnChanges, OnInit, SimpleChanges, ViewChild} from '@angular/core';
import {
  ChatClient,
  ChatMessageDto,
  ChatSessionDto,
  SendMessageToChatSessionCommand,
  UpdateChatSessionLastReadCommand
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
  @Input() set session(value: ChatSessionDto) {
    this.sessionSubject.next(value);
  }

  get session(): ChatSessionDto {
    return this.sessionSubject.value;
  }

  @ViewChild('textArea') private textArea: ElementRef;
  public messages$: Observable<ChatMessageDto[]>;
  private gettingMoreMessages = false;
  private gotAllMessages = false;
  private sessionSubject = new BehaviorSubject<ChatSessionDto>(null);

  constructor(private chatService: ChatFacade, private chatClient: ChatClient) {

  }

  ngOnInit(): void {
    this.messages$ = this.sessionSubject.asObservable().pipe(
      switchMap(session => {
          if (session) {
            return this.chatService.getChatMessages$(session.id).pipe(tap((a) => {
              this.chatService.updateLastRead(session);
            }));
          }
          return of([]);
        }
      ));
  }


  onScroll(event: any) {
    if (event.target.scrollHeight + event.target.scrollTop - event.target.clientHeight < 100) {
      if (!this.gettingMoreMessages && !this.gotAllMessages) {
        this.gettingMoreMessages = true;
        this.chatService.loadMoreMessages(this.session.id).then((amount) => {
          this.gettingMoreMessages = false;
          if (amount === 0) {
            this.gotAllMessages = true;
          }
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
    })).subscribe(() => {
      this.textArea.nativeElement.value = '';
    });
  }

}

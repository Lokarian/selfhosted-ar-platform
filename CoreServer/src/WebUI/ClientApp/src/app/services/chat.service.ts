import {Injectable} from '@angular/core';
import {ChatClient, ChatMessageDto, ChatSessionDto} from "../web-api-client";
import {BehaviorSubject, firstValueFrom} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private sessionSubject: BehaviorSubject<ChatSessionDto[]> = new BehaviorSubject<ChatSessionDto[]>([]);
  private sessionsInitialized = false;
  //store behavior subject for each chat session
  private messageStore: { [key: string]: BehaviorSubject<ChatMessageDto[]> } = {};

  constructor(private chatClient: ChatClient) {
  }

  public get chatSessions$() {
    if (!this.sessionsInitialized) {
      this.chatClient.getMyChatSessions().subscribe(sessions => {
        sessions.forEach(session => {
          if (!this.messageStore[session.id]) {
            this.messageStore[session.id] = new BehaviorSubject<ChatMessageDto[]>(session.lastMessage ? [session.lastMessage] : []);
          }
        });
        this.sessionSubject.next(sessions);
      });
      this.sessionsInitialized = true;
    }
    return this.sessionSubject.asObservable();
  }

  public getChatMessages$(chatSessionId: string) {
    if (!this.messageStore[chatSessionId]) {
      this.messageStore[chatSessionId] = new BehaviorSubject<ChatMessageDto[]>([]);
    }
    return this.messageStore[chatSessionId].asObservable();
  }

  addChatSession(chatSession: ChatSessionDto) {
    if (!this.messageStore[chatSession.id]) {
      this.messageStore[chatSession.id] = new BehaviorSubject<ChatMessageDto[]>(chatSession.lastMessage ? [chatSession.lastMessage] : []);
    }
    this.sessionSubject.next([...this.sessionSubject.value, chatSession]);
  }

  addChatMessage(chatMessage: ChatMessageDto) {
    this.messageStore[chatMessage.sessionId].next(this.insertChatMessageIntoArray(chatMessage, this.messageStore[chatMessage.sessionId].value));
  }

  /**
   * Insert a chat message into an array in correct order, newest first
   * @param chatMessage
   * @param chatMessages
   * @private
   */
  private insertChatMessageIntoArray(chatMessage: ChatMessageDto, chatMessages: ChatMessageDto[]): ChatMessageDto[] {
    //if it already exists, swap it out with the new one. if it doesn't exist, insert it sorted by sentAt timestamp descending
    const index = chatMessages.findIndex(m => m.id === chatMessage.id);
    if (index > -1) {
      chatMessages[index] = chatMessage;
    } else {
      let index = chatMessages.findIndex(m => m.sentAt < chatMessage.sentAt);
      if (index === -1) {
        index = chatMessages.length;
      }
      chatMessages.splice(index, 0, chatMessage);
    }
    return chatMessages
  }

  public async loadMoreMessages(chatSessionId: string, amount = 20) {
    var earliestMessageTimestamp = this.messageStore[chatSessionId].value.length > 0 ? this.messageStore[chatSessionId].value[this.messageStore[chatSessionId].value.length - 1].sentAt : undefined
    const messages = await firstValueFrom(this.chatClient.getChatMessages(chatSessionId,earliestMessageTimestamp, amount));
    this.messageStore[chatSessionId].next([...messages, ...this.messageStore[chatSessionId].value]);
    return;
  }

}

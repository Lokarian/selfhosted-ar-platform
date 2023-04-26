import {Injectable} from '@angular/core';
import {
  ChatClient,
  ChatMemberDto,
  ChatMessageDto,
  ChatSessionDto,
  UpdateChatSessionLastReadCommand
} from "../web-api-client";
import {BehaviorSubject, firstValueFrom} from "rxjs";
import {CurrentUserService} from "./user/current-user.service";

@Injectable({
  providedIn: 'root'
})
export class ChatFacade {
  private sessionSubject: BehaviorSubject<ChatSessionDto[]> = new BehaviorSubject<ChatSessionDto[]>([]);
  private sessionsInitialized = false;
  //store behavior subject for each chat session
  private messageStore: { [key: string]: BehaviorSubject<ChatMessageDto[]> } = {};

  constructor(private chatClient: ChatClient, private currentUserService: CurrentUserService) {
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

  updateChatSession(chatSession: ChatSessionDto) {
    //if the current user is not a member of the session, remove the session and messestore
    if (!chatSession.members.some(m => m.userId === this.currentUserService.user.id)) {
      this.sessionSubject.next(this.sessionSubject.value.filter(s => s.id !== chatSession.id));
      delete this.messageStore[chatSession.id];
      return;
    }
    if (!this.messageStore[chatSession.id]) {
      this.messageStore[chatSession.id] = new BehaviorSubject<ChatMessageDto[]>(chatSession.lastMessage ? [chatSession.lastMessage] : []);
    }
    if (!chatSession.lastMessage) {
      chatSession.lastMessage = this.messageStore[chatSession.id].value[0];
    }
    this.sessionSubject.next([...this.sessionSubject.value.filter(s => s.id !== chatSession.id), chatSession]);
  }

  addChatMessage(chatMessage: ChatMessageDto) {
    this.messageStore[chatMessage.sessionId].next(this.insertChatMessageIntoArray(chatMessage, this.messageStore[chatMessage.sessionId].value));
    //update the session latest message
    const session = this.sessionSubject.value.find(s => s.id === chatMessage.sessionId);
    if (session) {
      session.lastMessage = chatMessage;
      this.sessionSubject.next([...this.sessionSubject.value.filter(s => s.id !== session.id), session]);

    }
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
    const messages = await firstValueFrom(this.chatClient.getChatMessages(chatSessionId, earliestMessageTimestamp, amount));
    var currentMessages = this.messageStore[chatSessionId].value;
    var newMessages = messages.reduce((acc, m) => this.insertChatMessageIntoArray(m, acc), currentMessages);
    this.messageStore[chatSessionId].next(newMessages);
    return messages.length;
  }

  updateChatMember(chatMember: ChatMemberDto) {
    const session = this.sessionSubject.value.find(s => s.id === chatMember.sessionId);
    if (!session) {
      return;
    }
    session.members = session.members.filter(m => m.userId !== chatMember.userId);
    session.members.push(chatMember);
    this.sessionSubject.next([...this.sessionSubject.value.filter(s => s.id !== session.id), session]);
  }

  updateLastRead(session: ChatSessionDto) {
    this.chatClient.updateLastRead(new UpdateChatSessionLastReadCommand({chatSessionId: session.id})).subscribe();
    session = this.sessionSubject.value.find(s => s.id === session.id);
    if (!session) {
      return;
    }
    const myMember = session.members.find(m => m.userId === this.currentUserService.user.id);
    if (!myMember) {
      return;
    }
    myMember.lastSeen = new Date();
    this.sessionSubject.next([...this.sessionSubject.value.filter(s => s.id !== session.id), session]);

  }
}

import {Injectable} from '@angular/core';
import {
  ChatClient,
  ChatMemberDto,
  ChatMessageDto,
  ChatSessionDto, CreateChatSessionCommand,
  UpdateChatSessionLastReadCommand
} from "../web-api-client";
import {BehaviorSubject, firstValueFrom, Observable, share, switchMap, tap} from "rxjs";
import {CurrentUserService} from "./user/current-user.service";
import {SessionFacade} from "./session-facade.service";
import {filter, first, map} from "rxjs/operators";
import {SignalRConnectionState, SignalRService} from "./signalr.service";

@Injectable({
  providedIn: 'root'
})
export class ChatFacade {
  private messageStore: { [key: string]: BehaviorSubject<ChatMessageDto[]> } = {};

  //sessionSubject: object with string key and ReplaySubject<SessionDto> value
  private sessionSubjects: { [key: string]: BehaviorSubject<ChatSessionDto | undefined> } = {};
  private sessionsSubject = new BehaviorSubject<BehaviorSubject<ChatSessionDto | undefined>[]>([]);
  public sessionObservables$: Observable<Observable<ChatSessionDto | undefined>[]>;
  public sessions$: Observable<ChatSessionDto[]>;


  constructor(private chatClient: ChatClient, private sessionFacade: SessionFacade, private currentUserService: CurrentUserService,private signalRService: SignalRService) {
    sessionFacade.registerCapabilityFacadeResolver('chatSession', (this as any as SessionFacade));
    this.sessionObservables$ = this.sessionsSubject.asObservable().pipe(map(sessions => sessions.map(s => s.asObservable())));

    this.sessions$ = this.sessionsSubject.asObservable().pipe(map(sessions => sessions.map(s => s.value)));
    this.signalRService.connectionState$.pipe(filter(state => state === SignalRConnectionState.Connected)).subscribe(() => this.init());

  }
  init(){
    this.chatClient.getMyChatSessions().subscribe(sessions => {
      sessions.forEach(session => this.addOrReplaceSession(session));
    });
  }

  public get sessions() {
    return this.sessionsSubject.value;
  }

  public session$(sessionId: string) {
    const existingSession = this.sessionSubjects[sessionId];
    if (existingSession) {
      return this.sessionSubjects[sessionId].asObservable();
    }
    //return a new observable that will emit the session when it is added
    const observable = this.sessionsSubject.pipe(filter(sessions => sessions.some(s => s.value?.baseSessionId === sessionId)), map(sessions => sessions.find(s => s.value?.baseSessionId === sessionId)));
    return observable.pipe(first(), switchMap(s => s.asObservable()));
  }

  public session(sessionId: string) {
    return this.sessionSubjects[sessionId]?.value;
  }

  public addOrReplaceSession(session: ChatSessionDto) {
    if (this.sessionSubjects[session.baseSessionId]) {
      let existingSession = this.sessionSubjects[session.baseSessionId].value;
      existingSession = Object.assign(existingSession, session);
      this.sessionSubjects[session.baseSessionId].next(existingSession);
    } else {
      session = this.wrapInProxy(session);
      this.sessionSubjects[session.baseSessionId] = new BehaviorSubject<ChatSessionDto>(session);
      this.sessionsSubject.next([...this.sessionsSubject.value, this.sessionSubjects[session.baseSessionId]]);
    }

  }


  public createChatSession(id: string) {
    const obs = this.chatClient.createChatSession(new CreateChatSessionCommand({sessionId:id})).pipe(tap(session => {
      this.addOrReplaceSession(session);
    }), share());
    obs.subscribe();
    return obs;
  }

  addChatMessage(chatMessage: ChatMessageDto) {
    if (!this.messageStore[chatMessage.sessionId]) {
      this.messageStore[chatMessage.sessionId] = new BehaviorSubject<ChatMessageDto[]>([]);
    }
    const updatedChatArray = this.insertChatMessageIntoArray(chatMessage, this.messageStore[chatMessage.sessionId].value);
    this.messageStore[chatMessage.sessionId].next(updatedChatArray);
    //update the sessions latest message
    const session = this.sessionSubjects[chatMessage.sessionId].value;
    if (!session) return;
    session.lastMessage = this.messageStore[chatMessage.sessionId].value[0];

    //instantly update the last read if the session is observed
    if (this.messageStore[chatMessage.sessionId].observed) {
      this.updateLastRead(chatMessage.sessionId);
    }

    this.sessionSubjects[chatMessage.sessionId].next(session);
  }

  public chatMessages$(sessionId: string) {
    if (!this.messageStore[sessionId]) {
      this.messageStore[sessionId] = new BehaviorSubject<ChatMessageDto[]>([]);
    }
    return this.messageStore[sessionId].asObservable();
  }


  public async loadMoreMessages(sessionId: string, amount = 20) {
    if (!this.messageStore[sessionId]) {
      this.messageStore[sessionId] = new BehaviorSubject<ChatMessageDto[]>([]);
    }
    const earliestMessageTimestamp = this.messageStore[sessionId].value.length > 0 ? this.messageStore[sessionId].value[this.messageStore[sessionId].value.length - 1].sentAt : undefined
    const messages = await firstValueFrom(this.chatClient.getChatMessages(sessionId, earliestMessageTimestamp, amount));
    const currentMessages = this.messageStore[sessionId].value;
    const newMessages = messages.reduce((acc, m) => this.insertChatMessageIntoArray(m, acc), currentMessages);
    this.messageStore[sessionId].next(newMessages);
    return messages.length;
  }

  updateChatMember(chatMember: ChatMemberDto) {
    //add or replace the member in the session if the session exists and update the subject
    const sessionSubject = this.sessionSubjects[chatMember.sessionId];
    if (!sessionSubject) {
      //we probably got freshly added to an existing session, so we need to load the session
      this.chatClient.getMyChatSessions().subscribe(sessions => {
        sessions.forEach(session => this.addOrReplaceSession(session));
      });
      return;
    }
    const session = sessionSubject.value;
    if (!session) return;
    const existingMember = session.members.find(m => m.userId === chatMember.userId);
    if (!existingMember) {
      session.members.push(chatMember);
    } else {
      let newMember = Object.assign(existingMember, chatMember);
      session.members = session.members.map(m => m.userId === newMember.userId ? newMember : m);
    }
    sessionSubject.next(session);
  }

  updateLastRead(sessionId: string) {
    this.chatClient.updateLastRead(new UpdateChatSessionLastReadCommand({sessionId})).subscribe();
    //instantly update the session subject
    const sessionSubject = this.sessionSubjects[sessionId];
    if (!sessionSubject) return;
    const session = sessionSubject.value;
    if (!session) return;
    session.members = session.members.map(m => m.userId === this.currentUserService.user.id ? {
      ...m,
      lastSeen: new Date()
    } as ChatMemberDto : m);
    sessionSubject.next(session);
  }

  public removeSession(sessionId: string) {
    if (this.sessionSubjects[sessionId]) {
      this.sessionSubjects[sessionId].complete();
      delete this.sessionSubjects[sessionId];

      const newSessionSubjects = Object.entries(this.sessionSubjects).filter(([key, _]) => key !== sessionId).map(([_, value]) => value);
      this.sessionsSubject.next(newSessionSubjects);
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

  private wrapInProxy(chatSession: ChatSessionDto) {
    return new Proxy(chatSession, {
      get: (target, prop) => {
        if (prop === 'baseSession') {
          return this.sessionFacade.session(target.baseSessionId);
        }
        return Reflect.get(target, prop);
      }
    })
  }
}



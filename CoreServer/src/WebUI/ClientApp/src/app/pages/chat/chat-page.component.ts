import {Component, OnInit} from '@angular/core';
import {ChatFacade} from "../../services/chat-facade.service";
import {
  AppUserDto,
  ChatClient,
  ChatSessionDto,
  CreateChatSessionCommand, CreateSessionCommand, SessionClient, SessionDto, UpdateSessionCommand, VideoSessionDto,
} from "../../web-api-client";
import {CurrentUserService} from "../../services/user/current-user.service";
import {BehaviorSubject, Observable, switchMap, tap} from "rxjs";
import {filter, map} from "rxjs/operators";
import {SessionFacade} from "../../services/session-facade.service";
import {VideoFacade} from "../../services/video-facade.service";

@Component({
  selector: 'app-chat-page',
  templateUrl: './chat-page.component.html',
  styleUrls: ['./chat-page.component.css']
})
export class ChatPageComponent implements OnInit {
  public sessions$: Observable<ChatSessionDto[]>;
  public selectedSessionSubject= new BehaviorSubject<ChatSessionDto>(null);
  public selectedSession$=this.selectedSessionSubject.asObservable().pipe(filter(s=>!!s));
  public videoSession:VideoSessionDto|undefined;
  public get selectedSession(){
    return this.selectedSessionSubject.value;
  }
  public set selectedSession(value){
    this.selectedSessionSubject.next(value);
  }
  public selectedSessionUserIds: string[] = [];
  public isEdit = false;

  constructor(private chatFacade: ChatFacade,
              private currentUserService: CurrentUserService,
              private chatClient: ChatClient,
              private sessionFacade: SessionFacade,
              private videoFacade: VideoFacade,
              private sessionClient: SessionClient) {

    this.sessions$ = this.chatFacade.sessions$.pipe(tap(sessions => {
      const session = sessions.find(s => s.baseSessionId === this.selectedSession?.baseSessionId);
      this.selectedSession = session || null;
    }), map(sessions => sessions.sort((a, b) => {
      const aDate = a.lastMessage?.sentAt || a.baseSession?.createdAt || new Date(0);
      const bDate = b.lastMessage?.sentAt || b.baseSession?.createdAt || new Date(0);
      return bDate.getTime() - aDate.getTime();
    })));
  }

  ngOnInit(): void {
  }

  getRepresentingUserIdInSession(session: ChatSessionDto) {
    return session.baseSession?.members.filter(m => m.userId !== this.currentUserService.user.id)[0]?.userId
      || this.currentUserService.user.id;
  }

  getUserIdsForNameDisplay(session: ChatSessionDto) {
    const otherUsers = session.baseSession?.members.filter(m => m.userId !== this.currentUserService.user.id) ?? [];
    return otherUsers.length ? otherUsers.map(m => m.userId) : [this.currentUserService.user.id];
  }

  createSession(users: AppUserDto[]) {
    this.sessionFacade.createSession(new CreateSessionCommand({userIds: users.map(u => u.id)})).subscribe(session => {
      this.chatFacade.createChatSession(session.id).subscribe();
    });
  }

  changeName(value: string) {
    this.sessionClient.updateSession(new UpdateSessionCommand({
      sessionId: this.selectedSession?.baseSessionId,
      name: value
    })).subscribe();
  }

  changeUsers(users: AppUserDto[]) {
    this.sessionClient.updateSession(new UpdateSessionCommand({
      sessionId: this.selectedSession?.baseSessionId,
      userIds: users.map(u => u.id)
    })).subscribe();
  }

  hasUnreadMessage(session: ChatSessionDto) {
    if (!session.lastMessage) {
      return false;
    }
    const myMember = session.members.find(m => m.userId === this.currentUserService.user.id);
    if (session.lastMessage.senderId === this.currentUserService.user.id) {
      return false;
    }
    return session.lastMessage.sentAt > (myMember?.lastSeen??new Date(0));
  }

  public selectSession(session: ChatSessionDto) {
    this.selectedSession = session;
    this.selectedSessionUserIds= this.sessionFacade.session(this.selectedSession?.baseSessionId).members.map(m => m.userId);
  }


  joinVideoSession(videoSession: VideoSessionDto) {
    this.videoSession = videoSession;
  }
}

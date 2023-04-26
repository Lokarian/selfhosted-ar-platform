import {Component, OnInit} from '@angular/core';
import {ChatFacade} from "../../services/chat-facade.service";
import {
  AppUserDto,
  ChatClient,
  ChatSessionDto,
  CreateChatSessionCommand,
  UpdateChatSessionCommand
} from "../../web-api-client";
import {CurrentUserService} from "../../services/user/current-user.service";
import {Observable, tap} from "rxjs";
import {map} from "rxjs/operators";

@Component({
  selector: 'app-chat-page',
  templateUrl: './chat-page.component.html',
  styleUrls: ['./chat-page.component.css']
})
export class ChatPageComponent implements OnInit {
  public sessions$: Observable<ChatSessionDto[]>
  M
  public selectedSession: ChatSessionDto | null = null;
  public isEdit = false;

  constructor(private chatService: ChatFacade, private currentUserService: CurrentUserService, private chatClient: ChatClient) {
    //get sessions from chatService sorted by last lastMessage?.sentAt, if date is null or undefined, put at the end
    this.sessions$ = this.chatService.chatSessions$.pipe(tap(sessions=>{
      const session=sessions.find(s=>s.id===this.selectedSession?.id);
      this.selectedSession=session||null;
    }),map(sessions => sessions.sort((a, b) => {
      const aDate = a.lastMessage?.sentAt || a.createdAt;
      const bDate = b.lastMessage?.sentAt || b.createdAt;
      return bDate.getTime() - aDate.getTime();
    })));
  }

  ngOnInit(): void {
  }

  getRepresentingUserIdInSession(session: ChatSessionDto) {
    return session.members.filter(m => m.userId !== this.currentUserService.user.id)[0]?.userId
      || this.currentUserService.user.id;
  }

  getUserIdsForNameDisplay(session: ChatSessionDto) {
    const otherUsers = session.members.filter(m => m.userId !== this.currentUserService.user.id);
    return otherUsers.length ? otherUsers.map(m => m.userId) : [this.currentUserService.user.id];
  }

  createSession(users: AppUserDto[]) {
    this.chatClient.createChatSession(new CreateChatSessionCommand({
      userIds: users.map(u => u.id)
    })).subscribe();
  }

  changeName(value: string) {
    this.chatClient.updateChatSession(new UpdateChatSessionCommand({
      name: value,
      sessionId: this.selectedSession?.id
    })).subscribe((session) => {
      this.chatService.updateChatSession(session);
    });
  }

  changeUsers(users: AppUserDto[]) {
    this.chatClient.updateChatSession(new UpdateChatSessionCommand({
      userIds: users.map(u => u.id),
      sessionId: this.selectedSession?.id
    })).subscribe((session) => {
      this.chatService.updateChatSession(session);
    });
  }

  hasUnreadMessage(session: ChatSessionDto) {
    if (!session.lastMessage) {
      return false;
    }
    const myMember = session.members.find(m => m.userId === this.currentUserService.user.id);
    if (session.lastMessage.senderId === this.currentUserService.user.id) {
      return false;
    }
    return session.lastMessage.sentAt > myMember?.lastSeen;
  }

  get selectedSessionUserIds() {
    return this.selectedSession?.members.map(m => m.userId) || [];
  }

}

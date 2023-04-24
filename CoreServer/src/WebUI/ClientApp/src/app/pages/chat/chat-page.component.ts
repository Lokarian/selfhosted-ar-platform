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

@Component({
  selector: 'app-chat-page',
  templateUrl: './chat-page.component.html',
  styleUrls: ['./chat-page.component.css']
})
export class ChatPageComponent implements OnInit {
  public sessions: ChatSessionDto[] = [];
  public selectedSession: ChatSessionDto | null = null;
  public isEdit=false;

  constructor(private chatService: ChatFacade, private currentUserService: CurrentUserService,private chatClient: ChatClient) {
    this.chatService.chatSessions$.subscribe(sessions => {
      this.sessions = sessions;
    });
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
    })).subscribe((session)=>{
      this.chatService.addChatSession(session);
    });
  }
  get selectedSessionUserIds(){
    return this.selectedSession?.members.map(m=>m.userId) || [];
  }

}

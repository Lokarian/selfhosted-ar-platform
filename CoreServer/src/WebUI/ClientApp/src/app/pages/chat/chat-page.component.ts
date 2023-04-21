import {Component, OnInit} from '@angular/core';
import {ChatService} from "../../services/chat.service";
import {ChatSessionDto} from "../../web-api-client";
import {CurrentUserService} from "../../services/user/current-user.service";

@Component({
  selector: 'app-chat-page',
  templateUrl: './chat-page.component.html',
  styleUrls: ['./chat-page.component.css']
})
export class ChatPageComponent implements OnInit {
  public sessions: ChatSessionDto[] = [];
  public selectedSession: ChatSessionDto | null = null;

  constructor(private chatService: ChatService, private currentUserService: CurrentUserService) {
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
}

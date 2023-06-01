import {Component, OnDestroy, OnInit} from '@angular/core';
import {Observable, ReplaySubject} from "rxjs";
import {AppUserDto, CreateSessionCommand, SessionDto} from "../../web-api-client";
import {SessionFacade} from "../../services/session-facade.service";
import {ChatFacade} from "../../services/chat-facade.service";
import {VideoFacade} from "../../services/video-facade.service";
import {ArFacade} from "../../services/ar-facade.service";

@Component({
  selector: 'app-session-list',
  templateUrl: './session-list.component.html',
  styleUrls: ['./session-list.component.css']
})
export class SessionListPageComponent implements OnInit,OnDestroy {
  sessions$: Observable<Observable<SessionDto>[]>;

  private destroyed$: ReplaySubject<boolean> = new ReplaySubject(1);

  constructor(private sessionFacade: SessionFacade, private chatFacade: ChatFacade, private videoFacade: VideoFacade, private arFacade: ArFacade) {
    this.sessions$ = this.sessionFacade.sessions$;
  }

  ngOnInit(): void {
  }


  ngOnDestroy(): void {
    this.destroyed$.next(true);
    this.destroyed$.complete();
  }

  public createChatSession(session: SessionDto) {
    this.chatFacade.createChatSession(session.id);
  }

  public createVideoSession(session: SessionDto) {
    this.videoFacade.createVideoSession(session.id);
  }

  public createSession(users: AppUserDto[]) {
    this.sessionFacade.createSession(new CreateSessionCommand({userIds: users.map(u => u.id)}));
  }

  createArSession(session: SessionDto) {
    this.arFacade.createArSession(session.id);
  }

}

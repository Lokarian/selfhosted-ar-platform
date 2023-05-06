import {Component, OnDestroy, OnInit} from '@angular/core';
import {SessionFacade} from "../../services/session-facade.service";
import {Observable, ReplaySubject, takeUntil} from "rxjs";
import {ChatSessionDto, SessionDto, VideoSessionDto} from "../../web-api-client";
import {ChatFacade} from "../../services/chat-facade.service";
import {VideoFacade} from "../../services/video-facade.service";
import {Subject} from "@microsoft/signalr";

@Component({
  selector: 'app-session',
  templateUrl: './session-page.component.html',
  styleUrls: ['./session-page.component.css']
})
export class SessionPageComponent implements OnInit, OnDestroy {
  sessions$: Observable<Observable<SessionDto>[]>;

  private destroyed$: ReplaySubject<boolean> = new ReplaySubject(1);

  constructor(private sessionFacade: SessionFacade, private chatFacade: ChatFacade, private videoFacade: VideoFacade) {
    this.sessions$ = this.sessionFacade.sessions$;
  }

  ngOnInit(): void {
  }


  ngOnDestroy(): void {
    this.destroyed$.next(true);
    this.destroyed$.complete();
  }

}

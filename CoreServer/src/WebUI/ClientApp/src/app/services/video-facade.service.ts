import {Injectable} from '@angular/core';
import {BehaviorSubject, firstValueFrom, Observable, ReplaySubject, share, skip, switchMap, tap} from "rxjs";
import {NotificationService} from "./notification.service";
import {
  ChatMemberDto,
  ChatSessionDto,
  CreateVideoSessionCommand,
  CreateVideoStreamCommand,
  JoinVideoSessionCommand, LeaveVideoSessionCommand,
  VideoClient, VideoMemberDto,
  VideoSessionDto,
  VideoStreamDto
} from "../web-api-client";
import {SessionFacade} from "./session-facade.service";
import {filter, first, map} from "rxjs/operators";
import {SignalRConnectionState, SignalRService} from "./signalr.service";

@Injectable({
  providedIn: 'root'
})
export class VideoFacade {
  private sessionSubjects: { [key: string]: BehaviorSubject<VideoSessionDto | undefined> } = {};
  private sessionsSubject = new BehaviorSubject<BehaviorSubject<VideoSessionDto | undefined>[]>([]);
  public sessionObservables$: Observable<Observable<VideoSessionDto | undefined>[]>;
  public sessions$: Observable<VideoSessionDto[]>;

  public initialized$ = new ReplaySubject<boolean>(1);


  private accessKeyStore: { [key: string]: string } = {};

  public session$(sessionId: string): Observable<VideoSessionDto> {
    const existingSession = this.sessionSubjects[sessionId];
    if (existingSession) {
      return this.sessionSubjects[sessionId].asObservable();
    }
    //return a new observable that will emit the session when it is added
    const observable = this.sessionsSubject.pipe(
      filter(sessions => sessions.some(s => s.value?.baseSessionId === sessionId)),
      map(sessions => sessions.find(s => s.value?.baseSessionId === sessionId)));
    return observable.pipe(first(), switchMap(s => s.asObservable()));
  }

  public session(sessionId: string) {
    return this.sessionSubjects[sessionId]?.value;
  }

  constructor(private notificationService: NotificationService, private sessionFacade: SessionFacade, private videoClient: VideoClient, private signalRService: SignalRService) {
    sessionFacade.registerCapabilityFacadeResolver('videoSession', (this as any as SessionFacade));
    this.sessionObservables$ = this.sessionsSubject.asObservable().pipe(map(sessions => sessions.map(s => s.asObservable())))
    this.sessions$ = this.sessionsSubject.asObservable().pipe(map(sessions => sessions.map(s => s.value)));
    this.signalRService.connectionState$.pipe(filter(state => state === SignalRConnectionState.Connected)).subscribe(() => this.init());
  }
  init(){
    this.videoClient.getMyVideoSessions().pipe(tap(sessions => {
      sessions.forEach(session => this.addOrReplaceSession(session));
    }),tap(_=>this.initialized$.next(true))).subscribe();
  }
  public addOrReplaceSession(session: VideoSessionDto) {
    let changedToActive = false;

    if (this.sessionSubjects[session.baseSessionId]) {
      changedToActive = session.active && !this.sessionSubjects[session.baseSessionId].value?.active;
      session=this.wrapInProxy(session);
      this.sessionSubjects[session.baseSessionId].next(session);
    } else {
      session = this.wrapInProxy(session);
      this.sessionSubjects[session.baseSessionId] = new BehaviorSubject<ChatSessionDto>(session);
      this.sessionsSubject.next([...this.sessionsSubject.value, this.sessionSubjects[session.baseSessionId]]);
      changedToActive = session.active;
    }

    if (changedToActive) {
      this.notificationService.add({severity: 'info', message: `Incoming call`,autoClose:true});
    }
  }

  public storeAccessKey(sessionId: string, accessKey: string) {
    this.accessKeyStore[sessionId] = accessKey;
  }

  public getAccessKey(sessionId: string) {
    return this.accessKeyStore[sessionId];
  }

  updateVideoMember(videoMember: VideoMemberDto) {
    //add or replace the member in the session if the session exists and update the subject
    const sessionSubject = this.sessionSubjects[videoMember.sessionId];
    if (!sessionSubject) {
      //we probably got freshly added to an existing session, so we need to load the session
      this.videoClient.getMyVideoSessions().subscribe(sessions => {
        sessions.forEach(session => this.addOrReplaceSession(session));
      });
      return;
    }
    const session = sessionSubject.value;
    if (!session) return;

    if(videoMember.deletedAt){
      session.members = session.members.filter(m => m.userId !== videoMember.userId);
      sessionSubject.next(session);
      return;
    }
    const existingMember = session.members.find(m => m.userId === videoMember.userId);
    if (!existingMember) {
      session.members.push(videoMember);
    } else {
      session.members = session.members.map(m => m.userId === videoMember.userId ? videoMember : m);
    }
    sessionSubject.next(session);
  }


  public updateVideoStream(videoStream: VideoStreamDto) {
    //get video member by ownerId
    const sessionSubject = Object.entries(this.sessionSubjects).find(([_, value]) => value.value?.members.find(m => m.id === videoStream.ownerId))?.[1];
    if (!sessionSubject) {
      console.warn('Could not find session for video stream');
      return;
    }
    const session = sessionSubject.value;
    if (!session) return;
    session.streams = session.streams.filter(s => s.id !== videoStream.id);
    session.streams.push(videoStream);
    sessionSubject.next(session);
  }

  public joinVideoSession(sessionId: string) {
    const obs = this.videoClient.joinVideoSession(new JoinVideoSessionCommand({videoSessionId: sessionId})).pipe(share());
    obs.subscribe(
      (result) => {
        this.storeAccessKey(sessionId, result.item2);
        this.updateVideoMember(result.item1);
      }
    );
    return obs;
  }

  public requestVideoStream(sessionId: string) {
    return this.videoClient.requestVideoStream(new CreateVideoStreamCommand({videoStreamMemberId: sessionId})).pipe(tap(stream => {
        this.updateVideoStream(stream);
      }),
      share());
  }

  public createVideoSession(id: string) {
    const obs = this.videoClient.createVideoSession(new CreateVideoSessionCommand({sessionId: id})).pipe(tap(session => {
      this.addOrReplaceSession(session);
    }), share());
    obs.subscribe();
    return obs;
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

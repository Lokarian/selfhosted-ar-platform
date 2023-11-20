import {Injectable} from '@angular/core';
import {BehaviorSubject, firstValueFrom, Observable, ReplaySubject, share, skip, switchMap, tap} from "rxjs";
import {NotificationService} from "./notification.service";
import {
  ArClient, ArMemberDto,
  ArSessionDto, CreateArSessionCommand, JoinArSessionCommand,
} from "../web-api-client";
import {SessionFacade} from "./session-facade.service";
import {filter, first, map} from "rxjs/operators";
import {SignalRConnectionState, SignalRService} from "./signalr.service";

@Injectable({
  providedIn: 'root'
})
export class ArFacade {
  private sessionSubjects: { [key: string]: BehaviorSubject<ArSessionDto | undefined> } = {};
  private sessionsSubject = new BehaviorSubject<BehaviorSubject<ArSessionDto | undefined>[]>([]);
  public sessionObservables$: Observable<Observable<ArSessionDto | undefined>[]>;
  public sessions$: Observable<ArSessionDto[]>;

  public initialized$ = new ReplaySubject<boolean>(1);

  public session$(sessionId: string): Observable<ArSessionDto> {
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

  constructor(private notificationService: NotificationService, private sessionFacade: SessionFacade, private arClient: ArClient, private signalRService: SignalRService) {
    sessionFacade.registerCapabilityFacadeResolver('arSession', (this as any as SessionFacade));
    this.sessionObservables$ = this.sessionsSubject.asObservable().pipe(map(sessions => sessions.map(s => s.asObservable())))
    this.sessions$ = this.sessionsSubject.asObservable().pipe(map(sessions => sessions.map(s => s.value)));
    this.signalRService.connectionState$.pipe(filter(state => state === SignalRConnectionState.Connected)).subscribe(() => this.init());
  }

  init() {
    this.arClient.getMyArSessions().pipe(tap(sessions => {
      sessions.forEach(session => this.addOrReplaceSession(session));
    }), tap(() => this.initialized$.next(true))).subscribe();
  }

  public addOrReplaceSession(session: ArSessionDto) {
    if (this.sessionSubjects[session.baseSessionId]) {
      session = this.wrapInProxy(session);
      this.sessionSubjects[session.baseSessionId].next(session);
    } else {
      session = this.wrapInProxy(session);
      this.sessionSubjects[session.baseSessionId] = new BehaviorSubject<ArSessionDto>(session);
      this.sessionsSubject.next([...this.sessionsSubject.value, this.sessionSubjects[session.baseSessionId]]);
    }
  }

  updateArMember(arMember: ArMemberDto) {
    //add or replace the member in the session if the session exists and update the subject
    const sessionSubject = this.sessionSubjects[arMember.sessionId];
    if (!sessionSubject) {
      //we probably got freshly added to an existing session, so we need to load the session
      this.arClient.getMyArSessions().subscribe(sessions => {
        sessions.forEach(session => this.addOrReplaceSession(session));
      });
      return;
    }
    const session = sessionSubject.value;
    if (!session) return;

    if (arMember.deletedAt) {
      session.members = session.members.filter(m => m.userId !== arMember.userId);
      sessionSubject.next(session);
      return;
    }
    const existingMember = session.members.find(m => m.userId === arMember.userId);
    if (!existingMember) {
      session.members.push(arMember);
    } else {
      session.members = session.members.map(m => m.userId === arMember.userId ? arMember : m);
    }
    sessionSubject.next(session);
  }

  public joinArSession(sessionId: string) {
    const obs = this.arClient.joinArSession(new JoinArSessionCommand({arSessionId: sessionId})).pipe(share());
    obs.subscribe(
      (result) => {
        this.updateArMember(result);
      }
    );
    return obs;
  }


  public createArSession(id: string) {
    const obs = this.arClient.createArSession(new CreateArSessionCommand({sessionId: id})).pipe(tap(session => {
      this.addOrReplaceSession(session);
    }), share());
    obs.subscribe();
    return obs;
  }


  private wrapInProxy(arSession: ArSessionDto) {
    return new Proxy(arSession, {
      get: (target, prop) => {
        if (prop === 'baseSession') {
          return this.sessionFacade.session(target.baseSessionId);
        }
        return Reflect.get(target, prop);
      }
    })
  }
}

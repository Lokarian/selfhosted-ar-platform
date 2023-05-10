import {Injectable} from '@angular/core';
import {BehaviorSubject, share, switchMap, tap} from "rxjs";
import {CreateSessionCommand, SessionClient, SessionDto, SessionMemberDto} from "../web-api-client";
import {CurrentUserService} from "./user/current-user.service";
import {filter, first, map} from "rxjs/operators";
import {UserFacade} from "./user/user-facade.service";
import {SignalRConnectionState, SignalRService} from "./signalr.service";

@Injectable({
  providedIn: 'root'
})
export class SessionFacade {
  //sessionSubject: object with string key and ReplaySubject<SessionDto> value
  private sessionSubjects: { [key: string]: BehaviorSubject<SessionDto | undefined> } = {};
  private sessionsSubject = new BehaviorSubject<BehaviorSubject<SessionDto | undefined>[]>([]);
  public sessions$ = this.sessionsSubject.asObservable().pipe(map(sessions => sessions.map(s => s.asObservable())));
  private capabilityFacadeResolvers: { [key: string]: SessionFacade } = {};

  public get sessions() {
    return this.sessionsSubject.value;
  }

  public session$(sessionId: string) {
    const existingSession = this.sessionSubjects[sessionId];
    if (existingSession) {
      return this.sessionSubjects[sessionId].asObservable();
    }
    //return a new observable that will emit the session when it is added
    const observable = this.sessionsSubject.pipe(
      filter(sessions => sessions.some(s => s.value?.id === sessionId)),
      map(sessions => sessions.find(s => s.value?.id === sessionId)));
    return observable.pipe(first(), switchMap(s => s.asObservable()));
  }

  public session(sessionId: string): SessionDto | undefined {
    return this.sessionSubjects[sessionId]?.value;
  }

  constructor(private sessionClient: SessionClient, private userFacade: UserFacade, private currentUserService: CurrentUserService, private signalrService: SignalRService) {
    this.signalrService.connectionState$.pipe(filter(state => state === SignalRConnectionState.Connected)).subscribe(() => this.init());
  }

  init() {
    this.sessionClient.getMySessions().subscribe(sessions => {
      sessions.forEach(session => this.addOrReplaceSession(session));
    });
  }


  public addOrReplaceSession(session: SessionDto) {
    if (this.sessionSubjects[session.id]) {
      let existingSession = this.sessionSubjects[session.id].value;
      existingSession = Object.assign(existingSession, session);
      this.sessionSubjects[session.id].next(existingSession);
    } else {
      session = this.wrapInProxy(session);
      this.sessionSubjects[session.id] = new BehaviorSubject<SessionDto>(session);
      this.sessionsSubject.next([...this.sessionsSubject.value, this.sessionSubjects[session.id]]);
    }

  }

  public removeSession(sessionId: string) {
    if (this.sessionSubjects[sessionId]) {
      this.sessionSubjects[sessionId].complete();
      delete this.sessionSubjects[sessionId];

      const newSessionSubjects = Object.entries(this.sessionSubjects).filter(([key, _]) => key !== sessionId).map(([_, value]) => value);
      this.sessionsSubject.next(newSessionSubjects);
    }
  }

  public updateSessionMember(sessionMember: SessionMemberDto) {
    const sessionSubject = this.sessionSubjects[sessionMember.sessionId];
    if (!sessionSubject) return;
    const session = sessionSubject.value;
    if (!session) return;
    const existingMember = session.members.find(m => m.userId === sessionMember.userId);
    if (!existingMember) {
      session.members.push(sessionMember);
    } else {
      let newMember = Object.assign(existingMember, sessionMember);
      session.members = session.members.map(m => m.userId === newMember.userId ? newMember : m);
    }
    sessionSubject.next(session);
  }

  public createSession(command: CreateSessionCommand) {
    const obs = this.sessionClient.createSession(command).pipe(tap(session => {
      this.addOrReplaceSession(session);
    }), share());
    obs.subscribe();
    return obs;
  }

  public subjectiveSessionName(sessionId: string): string {
    const session = this.session(sessionId);
    //add up all the members' names with commas in between that are not the current user
    const otherMembers = session.members.filter(m => m.userId !== this.currentUserService.user.id);
    return otherMembers.map(m => this.userFacade.user(m.userId)?.userName ?? "").join(", ");
  }

  public registerCapabilityFacadeResolver(sessionType: string, resolver: SessionFacade) {
    this.capabilityFacadeResolvers[sessionType] = resolver;
  }

  private wrapInProxy(chatSession: SessionDto) {
    return new Proxy(chatSession, {
      get: (target, prop) => {
        if (Object.keys(this.capabilityFacadeResolvers).includes(prop as string)) {
          return this.capabilityFacadeResolvers[prop as string].session(target.id);
        }
        return Reflect.get(target, prop);
      }
    })
  }

}

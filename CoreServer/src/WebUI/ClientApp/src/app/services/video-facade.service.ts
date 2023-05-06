import {Injectable} from '@angular/core';
import {BehaviorSubject} from "rxjs";
import {VideoClient, VideoSessionDto} from "../web-api-client";
import {NotificationService} from "./notification.service";
import {CurrentUserService} from "./user/current-user.service";

@Injectable({
  providedIn: 'root'
})
export class VideoFacade {
  private _videoSessionSubject: BehaviorSubject<VideoSessionDto[]> = new BehaviorSubject<VideoSessionDto[]>([]);
  public videoSessions$ = this._videoSessionSubject.asObservable();
  private currentVideoSessionSubject: BehaviorSubject<VideoSessionDto | null> = new BehaviorSubject<VideoSessionDto | null>(null);
  public currentVideoSession$ = this.currentVideoSessionSubject.asObservable();

  constructor(private notificationService: NotificationService, private videoClient: VideoClient, private currentUserService: CurrentUserService) {
  }

  /*public updateVideoSession(videoSession: VideoSessionDto) {
    const videoSessions = this._videoSessionSubject.getValue();
    const index = videoSessions.findIndex(x => x.id === videoSession.id);
    var changedToActive = false;
    if (index === -1) {
      videoSessions.push(videoSession);
      changedToActive = videoSession.active;
    } else {
      videoSessions[index] = videoSession;
      changedToActive = videoSession.active && !videoSessions[index].active;
    }
    this._videoSessionSubject.next(videoSessions);
    if (changedToActive) {
      this.notificationService.add({severity: 'info', message: `Incoming call`});
    }
    //check if current session has to be updated
    const currentSession = this.currentVideoSessionSubject.getValue();
    if (currentSession && currentSession.baseSessionId === videoSession.baseSessionId) {
      this.currentVideoSessionSubject.next(videoSession);
    }

  }

  public updateVideoSessionMember(videoSessionMember: VideoSessionMemberDto) {
    var session = this._videoSessionSubject.value.find(x => x.members.some(m => m.id === videoSessionMember.id));
    if (!session) {
      console.log("tried to update video session member, but corresponding session not found");
      return;
    }
    var member = session.members.find(x => x.id === videoSessionMember.id);
    if (!member) {
      console.log("tried to update video session member, but corresponding member not found");
      return;
    }
    session.members[session.members.indexOf(member)] = videoSessionMember;
    this.updateVideoSession(session);
  }

  public updateVideoStream(videoStream: VideoStreamDto) {
    const session = this._videoSessionSubject.value.find(x => x.streams.some(s => s.id === videoStream.id));
    if (!session) {
      console.log("tried to update video stream, but corresponding session/member not found");
      return;
    }
    if (session.streams.some(x => x.id === videoStream.id)) {
      session.streams[session.streams.indexOf(session.streams.find(x => x.id === videoStream.id))] = videoStream;
    } else {
      session.streams.push(videoStream);
    }
    this.updateVideoSession(session);
  }*/

}

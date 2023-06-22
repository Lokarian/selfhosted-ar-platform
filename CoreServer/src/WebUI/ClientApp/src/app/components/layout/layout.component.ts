import {Component} from '@angular/core';
import {SignalRConnectionState, SignalRService} from "../../services/signalr.service";
import {NotificationService} from "../../services/notification.service";
import {RpcUserService} from "../../services/rpc/rpc-user.service";
import {RpcChatService} from "../../services/rpc/rpc-chat.service";
import {CurrentUserService} from "../../services/user/current-user.service";
import {filter, map, switchMap} from "rxjs/operators";
import {RpcVideoService} from "../../services/rpc/rpc-video.service";
import {RpcSessionService} from "../../services/rpc/rpc-session.service";
import {ChatFacade} from "../../services/chat-facade.service";
import {VideoFacade} from "../../services/video-facade.service";
import {ArFacade} from "../../services/ar-facade.service";

@Component({
  selector: 'app-layout',
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent {
  constructor(private signalrService: SignalRService,
              private currentUserService: CurrentUserService,
              private chatFacade: ChatFacade,
              private videoFacade: VideoFacade,
              private arFacade: ArFacade,
              private notificationService: NotificationService,
              private rpcUserService: RpcUserService,
              private rpcSessionService: RpcSessionService,
              private rpcVideoService: RpcVideoService,
              private rpcChatService: RpcChatService) {
    this.initSignalR();
  }

  public initSignalR() {
    this.signalrService.init();
  }

  public get loaded$() {
    return this.currentUserService.user$.pipe(filter(user => !!user), switchMap(() => this.signalrService.connectionState$), filter(state=>state==SignalRConnectionState.Connected), map(() => true));
  }

}

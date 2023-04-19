import {Component} from '@angular/core';
import {SignalRService} from "../../services/signalr.service";
import {NotificationService} from "../../services/notification.service";
import {RpcUserService} from "../../services/rpc/rpc-user.service";
import {RpcChatService} from "../../services/rpc/rpc-chat.service";

@Component({
  selector: 'app-layout',
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent {
  constructor(private signalrService: SignalRService, private notificationService: NotificationService,private rpcUserService: RpcUserService,private rpcChatService: RpcChatService) {
    this.initSignalR();
  }
  public initSignalR() {
    this.signalrService.init();
  }

}

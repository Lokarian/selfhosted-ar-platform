import {Component} from '@angular/core';
import {SignalrService} from "../../services/signalr.service";
import {NotificationService} from "../../services/notification.service";

@Component({
  selector: 'app-layout',
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent {
  constructor(private signalrService: SignalrService, private notificationService: NotificationService) {

  }
  public initSignalR() {
    this.signalrService.init();
  }

}

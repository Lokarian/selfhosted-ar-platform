import {Component, OnInit} from '@angular/core';
import {Notification, NotificationService} from "../../services/notification.service";

@Component({
  selector: 'app-notification',
  templateUrl: './notification.component.html',
  styleUrls: ['./notification.component.css']
})
export class NotificationComponent implements OnInit {

  public notifications: Notification[] = [];

  constructor(private notificationService: NotificationService) {
  }

  ngOnInit(): void {
    this.notificationService.getNotifications().subscribe(notification => {
      this.notifications.push(notification);
      if(notification.autoClose){
        setTimeout(() => this.removeNotification(notification), 3000);
      }
    });
  }

  removeNotification(notification: any) {
    this.notifications = this.notifications.filter(n => n !== notification);
  }
}

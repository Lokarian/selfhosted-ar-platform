import {Injectable} from '@angular/core';
import {BehaviorSubject, Observable, ReplaySubject} from "rxjs";


export interface Notification{
  severity: 'success' | 'info' | 'warn' | 'error';
  title?: string;
  message?: string;
  autoClose?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {

  private notificationSubject: ReplaySubject<Notification> = new ReplaySubject<Notification>();

  constructor() {
  }

  public add(message: Notification) {
    this.notificationSubject.next(message);
  }

  public getNotifications(): Observable<Notification> {
    return this.notificationSubject.asObservable();
  }
  public error(message:string){
    this.add({severity:'error',message:message,autoClose:true});
  }

}

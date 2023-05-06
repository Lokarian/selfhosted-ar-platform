import {Inject, Injectable} from '@angular/core';
import {API_BASE_URL} from "../web-api-client";
import {HubConnection, HubConnectionBuilder, Subject} from "@microsoft/signalr";
import {AuthorizeService} from "./auth/authorize.service";
import {BehaviorSubject, firstValueFrom, Observable, share} from "rxjs";
import {NotificationService} from "./notification.service";
import {filter} from "rxjs/operators";
import {MessagePackHubProtocol} from "@microsoft/signalr-protocol-msgpack";

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private _hubConnection: HubConnection;
  private ready = new BehaviorSubject(false);
  public ready$ = this.ready.asObservable();

  constructor(private authorizeService: AuthorizeService, private notificationService: NotificationService, @Inject(API_BASE_URL) private baseUrl?: string) {
  }

  public async init() {
    this._hubConnection = await new HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/api/hub`,
        {
          accessTokenFactory: async () => {
            return await firstValueFrom(this.authorizeService.getAccessToken().pipe(filter(token => token !== null)));
          }
        })
      //.withHubProtocol(new MessagePackHubProtocol())
      .build();
    this._hubConnection.onreconnecting((error) => this.onReconnecting(error));
    this._hubConnection.onreconnected((error) => this.onReconnected(error));
    this._hubConnection.onclose((error: any) => this.onClose(error));
    this._hubConnection.on("IRpcUserClient/UpdateUser", console.log);
    this._hubConnection.start().then(() => {
      this.ready.next(true);
      console.log('SignalR Connected!');
    });
  }

  public notifyServerOfServiceRegistration(serviceName: string) {
    console.log('Registering service ' + serviceName);
    this._hubConnection.invoke('RegisterService', serviceName);
  }

  public on<T>(methodName: string, callback: (data: T) => void) {

    this._hubConnection.on(methodName, (data: any) => {
      //data = this.renameAllKeysToCamelCase(data);
      data = this.parseAllDates(data);
      callback(data);
    });
  }
  public stream(method:string, observable: Observable<any>,...args: any[]) {
    const subject = new Subject();
    observable.subscribe({
      next: async (v) => {
        subject.next(v);
      },
      error: (v) => {
        subject.complete();
      },
      complete: () => {
        subject.complete();
      }
    });
    console.log("Publishing stream", subject, ...args);
    return this._hubConnection.send(method, subject, ...args);
  }
  public dataStream(id:string,observable:Observable<any>) {
    return this.stream("PublishStream",observable,id);
  }

  public getStream<T extends any>( id:string) {
    return new Observable<T>(
      observer => {
        const stream = this._hubConnection.stream("SubscribeToStream", id)
        const subscription = stream.subscribe(observer);
        return () => subscription.dispose();
      }
    ).pipe(share());
  };

  private onReconnecting(error: any) {
    console.log('Reconnecting', error);
  }

  private onReconnected(connectionId: any) {
    console.log('Reconnected', connectionId);
  }

  private onClose(error?: Error) {
    this.notificationService.add({
      severity: 'error',
      title: 'Connection lost',
    })
    console.log('Signalr connection closed', error);
  }

  private renameAllKeysToCamelCase(obj: any) {
    if (obj instanceof Array) {
      for (let i = 0; i < obj.length; i++) {
        obj[i] = this.renameAllKeysToCamelCase(obj[i]);
      }
    } else if (obj instanceof Object) {
      const keys = Object.keys(obj);
      for (let i = 0; i < keys.length; i++) {
        const key = keys[i];
        const val = obj[key];
        delete obj[key];
        const camelCaseKey = key.charAt(0).toLowerCase() + key.slice(1);
        obj[camelCaseKey] = this.renameAllKeysToCamelCase(val);
      }
    }
    return obj;
  }

  /**
   * parse all iso strings to dates in the object
   * @param obj
   * @private
   */
  private parseAllDates(obj: any) {
    if (obj instanceof Array) {
      for (let i = 0; i < obj.length; i++) {
        obj[i] = this.parseAllDates(obj[i]);
      }
    } else if (obj instanceof Object) {
      const keys = Object.keys(obj);
      for (let i = 0; i < keys.length; i++) {
        const key = keys[i];
        const val = obj[key];
        obj[key] = this.parseAllDates(val)
      }
    } else if (typeof obj === 'string' && obj.match(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z$/)) {
      obj = new Date(obj);
    }
    return obj;
  }
}

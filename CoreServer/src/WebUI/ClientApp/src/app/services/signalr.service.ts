import {Inject, Injectable} from '@angular/core';
import {API_BASE_URL} from "../web-api-client";
import {HubConnection, HubConnectionBuilder, Subject} from "@microsoft/signalr";
import {AuthorizeService} from "./auth/authorize.service";
import {BehaviorSubject, firstValueFrom, Observable, ReplaySubject, share, tap} from "rxjs";
import {NotificationService} from "./notification.service";
import {filter} from "rxjs/operators";
import {MessagePackHubProtocol} from "@microsoft/signalr-protocol-msgpack";
import {CurrentUserService} from "./user/current-user.service";

export enum SignalRConnectionState {
  Disconnected = 0,
  Connecting = 1,
  Connected = 2,
}

export interface TopicProxy<T> {
  stream: Observable<T>;
  subject: ReplaySubject<T>;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private _hubConnection: HubConnection;
  private connectionStateSubject = new BehaviorSubject(SignalRConnectionState.Disconnected);
  public connectionState$ = this.connectionStateSubject.asObservable();
  private services: string[] = [];

  constructor(private authorizeService: AuthorizeService,
              private notificationService: NotificationService,
              private currentUserService: CurrentUserService,
              @Inject(API_BASE_URL) private baseUrl?: string) {
  }

  public async init() {
    this._hubConnection = await new HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/api/hub`,
        {
          accessTokenFactory: async () => {
            return await firstValueFrom(this.authorizeService.getAccessToken().pipe(filter(token => token !== null)));
          }
        })
      .withHubProtocol(new MessagePackHubProtocol())
      .build();
    this._hubConnection.onreconnecting((error) => this.onReconnecting(error));
    this._hubConnection.onreconnected((error) => this.onReconnected(error));
    this._hubConnection.onclose((error: any) => this.onClose(error));
    this._hubConnection.start().then(async () => {
      this.connectionStateSubject.next(SignalRConnectionState.Connecting);
      const connectionId: string = await this._hubConnection.invoke("InitializeConnection", this.services);
      this.currentUserService.setConnectionId(connectionId);
      this.connectionStateSubject.next(SignalRConnectionState.Connected);
      console.log('SignalR Connected!');
    });
  }

  public registerService(serviceName: string) {
    console.log('Registering service ' + serviceName);
    if (this.connectionStateSubject.value === SignalRConnectionState.Connected) {
      this._hubConnection.invoke('RegisterService', serviceName);
    }
    this.services.push(serviceName);
  }


  public on<T>(methodName: string, callback: (data: T) => void) {

    this._hubConnection.on(methodName, (data: any) => {
      data = this.renameAllKeysToCamelCase(data);
      data = this.parseAllDates(data);
      callback(data);
    });
  }

  public stream(method: string, observable: Observable<any>, ...args: any[]) {
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

  public dataStream(topic: string, observable: Observable<any>) {
    return this.stream("PublishStream", observable, topic);
  }

  public getStream<T extends any>(topic: string) {
    return new Observable<T>(
      observer => {
        const stream = this._hubConnection.stream("SubscribeToTopic", topic)
        const subscription = stream.subscribe(observer);
        return () => subscription.dispose();
      }
    ).pipe(share());
  };

  public topicProxy<T>(topic: string): TopicProxy<T> {
    const publishSubject = new ReplaySubject<T>(1);
    /*publishSubject.subscribe({
          next: async (v) => {
            console.log("Publishing to Topic", topic, v);
          }
        });*/

    this.dataStream(topic, publishSubject);
    return {
      stream: this.getStream<T>(topic)/*.pipe(tap(data=>console.log("Receiving from Topic",topic,data)))*/,
      subject: publishSubject,
    }
  }


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

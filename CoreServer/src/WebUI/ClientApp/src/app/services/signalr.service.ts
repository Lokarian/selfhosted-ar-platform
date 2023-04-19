import {Injectable} from '@angular/core';
import {IRPCWebClient} from "../models/interfaces/RPCWebClient";
import {AppUserDto, ChatMessageDto, ChatSessionDto} from "../web-api-client";
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {MessagePackHubProtocol} from "@microsoft/signalr-protocol-msgpack";
import {AuthorizeService} from "./auth/authorize.service";
import {BehaviorSubject, firstValueFrom} from "rxjs";
import {NotificationService} from "./notification.service";

@Injectable({
  providedIn: 'root'
})
export class SignalrService implements IRPCWebClient {
  private _hubConnection: HubConnection;
  private ready = new BehaviorSubject(false);
  public ready$ = this.ready.asObservable();

  constructor(private authorizeService: AuthorizeService, private notificationService: NotificationService) {
  }

  public async init() {
    this._hubConnection = await new HubConnectionBuilder()
      .withUrl('https://localhost:5001/api/hub',
        {
          accessTokenFactory: async () => {
            return await firstValueFrom(this.authorizeService.getAccessToken());
          }
        })
      .withHubProtocol(new MessagePackHubProtocol())
      .build();
    this._hubConnection.onreconnecting((error) => this.onReconnecting(error));
    this._hubConnection.onreconnected((error) => this.onReconnected(error));
    this._hubConnection.onclose((error: any) => this.onClose(error));
    this.registerMethods();
    this._hubConnection.start().then(() => {
      this.ready.next(true);
      console.log('SignalR Connected!');
    });
  }

  public on<T>(methodName: string, callback: (data: T) => void) {
    this._hubConnection.on(methodName, callback);
  }

  private registerMethods() {
    this.on<ChatMessageDto>('NewChatMessage', (chatMessage: ChatMessageDto) => this.newChatMessage(chatMessage));
    this.on<ChatSessionDto>('UpdateChatSession', (chatSession: ChatSessionDto) => this.updateChatSession(chatSession));
  }

  private onReconnecting(error: any) {
    console.log('Reconnecting', error);
  }

  private onReconnected(connectionId: any) {
    console.log('Reconnected', connectionId);
  }

  private onClose(error?: Error) {
    console.log('Connection closed', error);
  }

  newChatMessage(chatMessage: ChatMessageDto) {
    console.log(chatMessage);
  }

  updateChatSession(chatSession: ChatSessionDto) {
    console.log(chatSession);
  }

  updateUser(user: AppUserDto) {
    console.log(user);
  }

}

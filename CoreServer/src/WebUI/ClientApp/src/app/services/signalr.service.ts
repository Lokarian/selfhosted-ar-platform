import { Injectable } from '@angular/core';
import {IRPCWebClient} from "../models/interfaces/RPCWebClient";
import {AppUserDto, ChatMessageDto, ChatSessionDto} from "../web-api-client";
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {MessagePackHubProtocol} from "@microsoft/signalr-protocol-msgpack";

@Injectable({
  providedIn: 'root'
})
export class SignalrService implements IRPCWebClient{
  private _hubConnection!: HubConnection;

  public init() {
    this._hubConnection = new HubConnectionBuilder()
      .withUrl('/api/hub')
      .withHubProtocol(new MessagePackHubProtocol())
      .build();
    this._hubConnection.start();
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

import { Injectable } from '@angular/core';
import {AppUserDto, ChatMessageDto, ChatSessionDto} from "../../web-api-client";
import {IRpcChatService} from "../../models/interfaces/RpcChatService";
import {RpcService} from "./rpc.service";
import {SignalRService} from "../signalr.service";

@Injectable({
  providedIn: 'root'
})
export class RpcChatService extends RpcService implements IRpcChatService{

  constructor(private signalRService:SignalRService) {
    super(signalRService);
  }

  NewChatMessage(chatMessage: ChatMessageDto) {
    console.log(chatMessage);
  }

  UpdateChatSession(chatSession: ChatSessionDto) {
    console.log(chatSession);
  }


}

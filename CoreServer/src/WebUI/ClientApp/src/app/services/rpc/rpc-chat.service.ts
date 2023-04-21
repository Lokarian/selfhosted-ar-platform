import { Injectable } from '@angular/core';
import { ChatMessageDto, ChatSessionDto} from "../../web-api-client";
import {IRpcChatService} from "../../models/interfaces/RpcChatService";
import {RpcService} from "./rpc.service";
import {SignalRService} from "../signalr.service";
import {ChatService} from "../chat.service";

@Injectable({
  providedIn: 'root'
})
export class RpcChatService extends RpcService implements IRpcChatService{

  constructor(private signalRService:SignalRService, private chatService: ChatService) {
    super(signalRService);
  }

  NewChatMessage(chatMessage: ChatMessageDto) {
    this.chatService.addChatMessage(chatMessage);
  }

  UpdateChatSession(chatSession: ChatSessionDto) {
    this.chatService.addChatSession(chatSession);
  }


}

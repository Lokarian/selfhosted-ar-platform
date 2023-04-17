import { Injectable } from '@angular/core';
import {IRPCWebClient} from "../../models/interfaces/RPCWebClient";
import {AppUserDto, ChatMessageDto, ChatSessionDto} from "../../web-api-client";

@Injectable({
  providedIn: 'root'
})
export class ChatRpcService implements IRPCWebClient{

  constructor() { }

  newChatMessage(chatMessage: ChatMessageDto) {
  }

  updateChatSession(chatSession: ChatSessionDto) {
  }

  updateUser(user: AppUserDto) {
  }
}

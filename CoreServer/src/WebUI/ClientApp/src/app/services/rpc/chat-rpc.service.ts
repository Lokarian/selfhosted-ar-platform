import { Injectable } from '@angular/core';
import {IRPCWebClient} from "../../models/interfaces/RPCWebClient";
import {AppUserDto, ChatMessageDto, ChatSessionDto} from "../../web-api-client";

@Injectable({
  providedIn: 'root'
})
export class ChatRpcService implements IRPCWebClient{

  constructor() { }

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

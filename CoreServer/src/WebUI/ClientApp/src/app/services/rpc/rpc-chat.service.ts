import {Injectable} from '@angular/core';
import {ChatMemberDto, ChatMessageDto, ChatSessionDto} from "../../web-api-client";
import {IRpcChatService} from "../../models/interfaces/RpcChatService";
import {RpcService} from "./rpc.service";
import {SignalRService} from "../signalr.service";
import {ChatFacade} from "../chat-facade.service";

@Injectable({
  providedIn: 'root'
})
export class RpcChatService extends RpcService implements IRpcChatService {

  constructor(private signalRService: SignalRService, private chatFacade: ChatFacade) {
    super(signalRService, "RpcChatService", {
      NewChatMessage: (chatMessage: ChatMessageDto) => this.NewChatMessage(chatMessage),
      UpdateChatSession: (chatSession: ChatSessionDto) => this.UpdateChatSession(chatSession),
      UpdateChatMember: (chatMember:ChatMemberDto) => this.UpdateChatMember(chatMember)
    });
  }

  NewChatMessage(chatMessage: ChatMessageDto) {
    this.chatFacade.addChatMessage(chatMessage);
  }

  UpdateChatSession(chatSession: ChatSessionDto) {
    this.chatFacade.addOrReplaceSession(chatSession)
  }
  UpdateChatMember(chatMember:ChatMemberDto){
    this.chatFacade.updateChatMember(chatMember);
  }

}

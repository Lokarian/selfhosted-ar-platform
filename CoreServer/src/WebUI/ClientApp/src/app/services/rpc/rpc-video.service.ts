import {Injectable} from '@angular/core';
import {ChatMemberDto, ChatMessageDto, ChatSessionDto} from "../../web-api-client";
import {IRpcChatService} from "../../models/interfaces/RpcChatService";
import {RpcService} from "./rpc.service";
import {SignalRService} from "../signalr.service";
import {ChatFacade} from "../chat-facade.service";
import {Observable} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class RpcVideoService extends RpcService {

  constructor(private signalRService: SignalRService, private chatService: ChatFacade) {
    super(signalRService, "RpcVideoService", {
    });
  }

  SendVideoStream(observable:Observable<any>){
    this.signalRService.stream("ReceiveVideoStream",observable);
  }

}

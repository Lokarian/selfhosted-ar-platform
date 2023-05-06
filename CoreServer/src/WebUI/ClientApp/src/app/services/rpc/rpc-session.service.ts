import {Injectable} from '@angular/core';
import {ChatMemberDto, ChatMessageDto, ChatSessionDto, SessionDto, SessionMemberDto} from "../../web-api-client";
import {IRpcChatService} from "../../models/interfaces/RpcChatService";
import {RpcService} from "./rpc.service";
import {SignalRService} from "../signalr.service";
import {ChatFacade} from "../chat-facade.service";
import {IRpcSessionService} from "../../models/interfaces/RpcSessionService";
import {SessionFacade} from "../session-facade.service";

@Injectable({
  providedIn: 'root'
})
export class RpcSessionService extends RpcService implements IRpcSessionService {

  constructor(private signalRService: SignalRService, private sessionFacade: SessionFacade) {
    super(signalRService, "RpcSessionService", {
      UpdateSession: (session: SessionDto) => this.UpdateSession(session),
      UpdateSessionMember: (sessionMember: SessionMemberDto) => this.UpdateSessionMember(sessionMember),
      RemoveSession: (id: string) => this.RemoveSession(id)
    });
  }

  UpdateSession(session: SessionDto) {
    console.log("UpdateSession", session);
    this.sessionFacade.addOrReplaceSession(session);
  }

  UpdateSessionMember(sessionMember: SessionMemberDto) {
    console.log("UpdateSessionMember", sessionMember);
    this.sessionFacade.updateSessionMember(sessionMember);
  }

  RemoveSession(id: string) {
    console.log("RemoveSession", id);
    this.sessionFacade.removeSession(id);
  }

}

import {Injectable} from '@angular/core';
import {
  ArMemberDto,
  ArSessionDto,
} from "../../web-api-client";
import {RpcService} from "./rpc.service";
import {SignalRService} from "../signalr.service";
import {from, Observable, ReplaySubject} from "rxjs";
import {map, mergeMap} from "rxjs/operators";
import {IRpcArService} from "../../models/interfaces/RpcArService";
import {ArFacade} from "../ar-facade.service";

@Injectable({
  providedIn: 'root'
})
export class RpcArService extends RpcService implements IRpcArService {

  constructor(private signalRService: SignalRService, private arFacade: ArFacade) {
    super(signalRService, "RpcArService", {
      UpdateArSession: (ArSession: ArSessionDto) => this.UpdateArSession(ArSession),
      UpdateArMember: (ArSessionMember: ArMemberDto) => this.UpdateArMember(ArSessionMember),
    });
  }

  UpdateArSession(ArSession: ArSessionDto) {
    this.arFacade.addOrReplaceSession(ArSession)
  }

  UpdateArMember(ArSessionMember: ArMemberDto) {
    this.arFacade.updateArMember(ArSessionMember);
  }
}

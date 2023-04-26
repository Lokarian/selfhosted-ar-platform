import {Injectable} from '@angular/core';
import {AppUserDto,} from "../../web-api-client";
import {IRpcUserService} from "../../models/interfaces/RpcUserService";
import {RpcService} from "./rpc.service";
import {SignalRService} from "../signalr.service";
import {UserFacade} from "../user/user-facade.service";
import {CurrentUserService} from "../user/current-user.service";

@Injectable({
  providedIn: 'root'
})
export class RpcUserService extends RpcService implements IRpcUserService {

  constructor(private signalRService: SignalRService, private userFacade: UserFacade, private currentUserService: CurrentUserService) {
    super(signalRService, "RpcUserService", {UpdateUser: (user: AppUserDto) => this.UpdateUser(user)});
  }

  UpdateUser(user: AppUserDto) {
    if (user.id === this.currentUserService.user.id) {
      this.currentUserService.setUser(user);
    }
    this.userFacade.updateUser(user);
  }
}

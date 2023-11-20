import {Injectable} from '@angular/core';
import {AppUserDto,} from "../../web-api-client";
import {IRpcUserService} from "../../models/interfaces/RpcUserService";
import {RpcService} from "./rpc.service";
import {SignalRService} from "../signalr.service";
import {UserFacade} from "../user/user-facade.service";
import {CurrentUserService} from "../user/current-user.service";
import {filter, first} from "rxjs/operators";
import {AuthorizeService} from "../auth/authorize.service";

@Injectable({
  providedIn: 'root'
})
export class RpcUserService extends RpcService implements IRpcUserService {

  constructor(private signalRService: SignalRService, private userFacade: UserFacade, private currentUserService: CurrentUserService,private authorize:AuthorizeService) {
    super(signalRService, "RpcUserService", {UpdateUser: (user: AppUserDto) => this.UpdateUser(user)});
  }

  UpdateUser(user: AppUserDto) {
    //when the app starts server may respond with the update to the current user before the current user is set via usercontroller.current(),
    //so we need to wait for the current user to be set before we update it
    this.currentUserService.user$.pipe(filter(u=>!!u),first()).subscribe(u=>{
      if (user.id === this.currentUserService.user.id) {
        this.currentUserService.setUser(user);
      }
    });
    this.userFacade.updateUser(user);
  }

}

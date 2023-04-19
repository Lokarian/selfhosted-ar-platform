import { Injectable } from '@angular/core';
import {AppUserDto,} from "../../web-api-client";
import {IRpcUserService} from "../../models/interfaces/RpcUserService";
import {RpcService} from "./rpc.service";
import {SignalRService} from "../signalr.service";

@Injectable({
  providedIn: 'root'
})
export class RpcUserService extends RpcService implements IRpcUserService{

  constructor(private signalRService: SignalRService) {
    console.log('RpcUserService');
    super(signalRService);
  }

  UpdateUser(user: AppUserDto) {
    console.log(user);
  }


}

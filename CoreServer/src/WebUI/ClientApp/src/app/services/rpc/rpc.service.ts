import {SignalRConnectionState, SignalRService} from "../signalr.service";
import {filter, first} from "rxjs/operators";

/**
 * base class for all rpc services.
 * rpc services need to adhere to the following naming convention:
 * - class name must start with Rpc and end with Service e.g. RpcUserService
 * - methods must the same name as the Server Interface without the "I" prefix
 */
export abstract class RpcService {
  constructor(private signalrService: SignalRService, private serviceName: string, methods: {
    [key: string]: Function
  }) {
    //loop over all methods in this class and register them with signalr
    signalrService.connectionState$.pipe(filter(state => state != SignalRConnectionState.Disconnected),first()).subscribe(() => {
      for (const methodName in methods) {
        this.signalrService.on(`${serviceName}/${methodName}`, (data) => methods[methodName](data));
      }
      signalrService.registerService(serviceName);
    });

  }
}

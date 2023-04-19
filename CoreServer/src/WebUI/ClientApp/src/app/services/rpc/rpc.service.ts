import {SignalRService} from "../signalr.service";
import {filter} from "rxjs/operators";

/**
 * base class for all rpc services.
 * rpc services need to adhere to the following naming convention:
 * - class name must start with Rpc and end with Service e.g. RpcUserService
 * - methods must the same name as the Server Interface without the "I" prefix
 */
export abstract class RpcService {
  constructor(private signalrService: SignalRService) {
    //loop over all methods in this class and register them with signalr
    signalrService.ready$.pipe(filter(ready => ready)).subscribe(() => {
      const className = this.constructor.name;
      for (const methodName of Object.getOwnPropertyNames(Object.getPrototypeOf(this))) {
        if (methodName !== 'constructor') {
          this.signalrService.on(`${className}/${methodName}`, (data) => this[methodName](data));
        }
      }
      signalrService.notifyServerOfServiceRegistration(className);
    });

  }
}

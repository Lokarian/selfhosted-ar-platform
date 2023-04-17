import { Pipe, PipeTransform } from '@angular/core';
import {UserFacadeService} from "./user-facade.service";
import {Observable} from "rxjs";
import {AppUserDto} from "../../web-api-client";

@Pipe({
  name: 'user'
})
export class UserPipe implements PipeTransform {

  constructor(private userFacade: UserFacadeService) {
  }
  transform(value: string, ...args: unknown[]): Observable<AppUserDto> {
    return this.userFacade.getUser$(value);
  }

}

import { Pipe, PipeTransform } from '@angular/core';
import {UserFacade} from "./user-facade.service";
import {Observable} from "rxjs";
import {AppUserDto} from "../../web-api-client";

@Pipe({
  name: 'user'
})
export class UserPipe implements PipeTransform {

  constructor(private userFacade: UserFacade) {
  }
  transform(value: string, ...args: unknown[]): Observable<AppUserDto> {
    return this.userFacade.getUser$(value);
  }

}

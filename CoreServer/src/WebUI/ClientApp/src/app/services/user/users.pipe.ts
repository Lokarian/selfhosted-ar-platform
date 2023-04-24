import {Pipe, PipeTransform} from '@angular/core';
import {Observable} from "rxjs";
import {AppUserDto} from "../../web-api-client";
import {UserFacade} from "./user-facade.service";

@Pipe({
  name: 'users'
})
export class UsersPipe implements PipeTransform {
  constructor(private userFacade: UserFacade) {
  }

  transform(value: string[], ...args: unknown[]): Observable<AppUserDto[]> {
    return this.userFacade.getUsers$(value);
  }

}

import { Injectable } from '@angular/core';
import {AppUser, UserClient} from "../web-api-client";
import {BehaviorSubject, firstValueFrom} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class CurrentUserService {
  private userSubject:BehaviorSubject<AppUser> = new BehaviorSubject<AppUser>(null);
  constructor(private userClient:UserClient) {

  }
  public get user$() {
    return this.userSubject.asObservable();
  }
  public async loadUser() {
    const user = await firstValueFrom(await this.userClient.current());
    //cast as AppUser

    this.userSubject.next(user);
  }

}

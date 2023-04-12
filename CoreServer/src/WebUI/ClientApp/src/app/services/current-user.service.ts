import {Injectable} from '@angular/core';
import {AppUser, UserClient} from "../web-api-client";
import {BehaviorSubject, firstValueFrom} from "rxjs";
import {AuthorizeService} from "./auth/authorize.service";

@Injectable({
  providedIn: 'root'
})
export class CurrentUserService {
  private userSubject: BehaviorSubject<AppUser> = new BehaviorSubject<AppUser>(null);

  constructor(private userClient: UserClient, private authService: AuthorizeService) {
    this.authService.getAccessToken().subscribe(token => {
      if (token) {
        this.loadUser();
      } else {
        this.userSubject.next(null);
      }
    });
  }

  public get user$() {
    return this.userSubject.asObservable();
  }

  public get user(): AppUser {
    return this.userSubject.value;
  }

  public async loadUser() {
    const user = await firstValueFrom(await this.userClient.current());
    this.userSubject.next(user);
  }

}

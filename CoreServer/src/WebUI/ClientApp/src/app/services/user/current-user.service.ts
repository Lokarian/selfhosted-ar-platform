import {Injectable} from '@angular/core';
import {AppUserDto, UserClient} from "../../web-api-client";
import {BehaviorSubject, firstValueFrom} from "rxjs";
import {AuthorizeService} from "../auth/authorize.service";

@Injectable({
  providedIn: 'root'
})
export class CurrentUserService {
  private userSubject: BehaviorSubject<AppUserDto> = new BehaviorSubject<AppUserDto>(null);
  private userConnectionIdSubject: BehaviorSubject<string> = new BehaviorSubject<string>(null);
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

  public get user(): AppUserDto {
    return this.userSubject.value;
  }

  public get userConnectionId$() {
    return this.userConnectionIdSubject.asObservable();
  }

  public get userConnectionId(): string {
    return this.userConnectionIdSubject.value;
  }


  public async loadUser() {
    try {
      const user = await firstValueFrom(await this.userClient.current());
      this.userSubject.next(user);
    }
    catch (e) {
      //if 404 then log out
      if (e.status === 404) {
        this.authService.removeAccessToken(true);
      }
    }

  }

  public setUser(user: AppUserDto) {
    this.userSubject.next(user);
  }

  public setConnectionId(connectionId: string) {
    this.userConnectionIdSubject.next(connectionId);
  }

}

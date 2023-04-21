import {Injectable} from '@angular/core';
import {Observable, ReplaySubject, share} from "rxjs";
import {AppUserDto, UserClient} from "../../web-api-client";

@Injectable({
  providedIn: 'root'
})
export class UserFacadeService {

  //dictionary of userIds to replay subjects
  private userCache: { [key: string]: ReplaySubject<AppUserDto> } = {};

  constructor(private userClient: UserClient) {
  }

  private initUser(id: string) {
    if (!this.userCache[id]) {
      this.userCache[id] = new ReplaySubject<AppUserDto>(1);
      this.userClient.getAppUserById(id).subscribe(user => {
        this.userCache[id].next(user);
      });
    }
  }

  public getUser$(id: string): Observable<AppUserDto> {
    this.initUser(id);
    return this.userCache[id].asObservable();
  }

  public UpdateUser(user: AppUserDto) {
    if (this.userCache[user.id]) {
      this.userCache[user.id].next(user);
    } else {
      this.userCache[user.id] = new ReplaySubject<AppUserDto>(1);
      this.userCache[user.id].next(user);
    }
  }
}

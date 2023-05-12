import {Injectable} from '@angular/core';
import {BehaviorSubject, firstValueFrom, Observable, ReplaySubject, share} from "rxjs";
import {AppUserDto, UserClient} from "../../web-api-client";
import {filter} from "rxjs/operators";

@Injectable({
  providedIn: 'root'
})
export class UserFacade {

  //dictionary of userIds to replay subjects
  private userCache: { [key: string]: BehaviorSubject<AppUserDto> } = {};

  constructor(private userClient: UserClient) {
  }

  private initUser(id: string) {
    if (!this.userCache[id]) {
      this.userCache[id] = new BehaviorSubject<AppUserDto>(undefined);
      this.userClient.getAppUserById(id).subscribe(user => {
        this.userCache[id].next(user);
      });
    }
  }

  public getUser$(id: string): Observable<AppUserDto> {
    if(!id){
      return new Observable<AppUserDto>(subscriber => {
      });
    }
    this.initUser(id);
    return this.userCache[id].asObservable();
  }

  public user(id: string): AppUserDto {
    return this.userCache[id]?.value;
  }

  public getUsers$(ids: string[]): Observable<AppUserDto[]> {
    const promises: Promise<AppUserDto>[] = ids.map(id => {
      this.initUser(id);
      return firstValueFrom(this.userCache[id].asObservable().pipe(filter(u => !!u)));
    });
    return new Observable<AppUserDto[]>(subscriber => {
      Promise.all(promises).then(users => {
        subscriber.next(users);
        subscriber.complete();
      });
    }).pipe(share());
  }


  public updateUser(user: AppUserDto) {
    if (this.userCache[user.id]) {
      this.userCache[user.id].next(user);
    } else {
      this.userCache[user.id] = new BehaviorSubject<AppUserDto>(user);
      this.userCache[user.id].next(user);
    }
  }
}

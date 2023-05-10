import {HostListener, Injectable} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';
import {filter, map} from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthorizeService {
  private tokenSubject: BehaviorSubject<string | null> = new BehaviorSubject<string | null>(null);
  constructor() {
    this.tokenSubject.next(this._getAccessToken());
  }

  public isAuthenticated(): Observable<boolean> {
    return this.tokenSubject.asObservable()
      .pipe(
        map(token => {
          //test if jwt token is expired
          if (!token) {
            return false;
          }
          const jwtData = token.split('.')[1];
          const decodedJwtJsonData = window.atob(jwtData);
          const decodedJwtData = JSON.parse(decodedJwtJsonData);
          const expirationDate = new Date(0);
          expirationDate.setUTCSeconds(decodedJwtData.exp);
          return expirationDate > new Date() || decodedJwtData.exp === undefined;
        })
      );
  }

  public getAuthorizationHeaderValue(): Observable<string | null> {
    return this.tokenSubject.asObservable()
      .pipe(
        filter(token => token !== null),
        map(token => `Bearer ${token}`)
      );
  }

  public getAccessToken(): Observable<string | null> {
    return this.tokenSubject.asObservable();
  }

  private _getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  public saveAccessToken(token: string) {
    localStorage.setItem('access_token', token);
    this.tokenSubject.next(token);
  }

  public removeAccessToken(triggerReload: boolean = false) {
    localStorage.removeItem('access_token');
    if (triggerReload) {
      window.location.reload();
    }
  }
}

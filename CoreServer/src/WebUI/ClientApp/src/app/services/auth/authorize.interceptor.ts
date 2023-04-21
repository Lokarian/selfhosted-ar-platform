import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import {Observable, take} from 'rxjs';
import { AuthorizeService } from './authorize.service';
import { mergeMap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthorizeInterceptor implements HttpInterceptor {
  constructor(private authorize: AuthorizeService) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return this.authorize.getAccessToken()
      .pipe(take(1),mergeMap(token => this.processRequestWithToken(token, req, next)));
  }

  // Checks if there is an access_token available in the authorize service
  // and adds it to the request in case it's targeted at the same origin as the
  // single page application.
  private processRequestWithToken(token: string | null, req: HttpRequest<any>, next: HttpHandler) {
    if (!!token && this.isSameOriginUrl(req)) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(req);
  }

  private isSameOriginUrl(req: any) {
    if (req.url.replace(/^\w+:\/\//, '').startsWith(`${window.location.hostname}`)) {
      return true;
    }

    // It's a protocol relative url with the same origin.
    // For example: //www.example.com/api/Products
    if (req.url.startsWith(`//${window.location.hostname}/`)) {
      return true;
    }

    // It's a relative url like /api/Products
    if (/^\/[^\/].*/.test(req.url)) {
      return true;
    }

    // It's an absolute or protocol relative url that
    // doesn't have the same origin.
    return false;
  }
}

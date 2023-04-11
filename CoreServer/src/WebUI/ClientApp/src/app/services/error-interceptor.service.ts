import {Injectable} from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor, HttpErrorResponse
} from '@angular/common/http';
import {catchError, Observable} from 'rxjs';
import {NotificationService} from "./notification.service";

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  constructor(private notificationService: NotificationService) {
  }

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    //on 400 error add notification
    return next.handle(request).pipe(catchError(async (err: HttpErrorResponse) => {
      if (err.status === 400 && err.error.type === "application/problem+json") {
        try {
          //parse err.error (which is blob) as json
          const json = JSON.parse(await err.error.text());
          if(json.errors){
            for (const [key,value] of Object.entries(json.errors)) {
              this.notificationService.add({
                severity: 'error',
                title: key,
                message: value as string,
                autoClose: true
              });
            }
          }

        } catch (e) {
        }
      }
      throw err;
    }));

  }
}

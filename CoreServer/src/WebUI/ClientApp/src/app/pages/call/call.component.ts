import { Component, OnInit } from '@angular/core';
import {Observable} from "rxjs";
import {SessionDto} from "../../web-api-client";
import {ActivatedRoute} from "@angular/router";
import {SessionFacade} from "../../services/session-facade.service";
import {ChatFacade} from "../../services/chat-facade.service";
import {VideoFacade} from "../../services/video-facade.service";
import {ArFacade} from "../../services/ar-facade.service";



@Component({
  selector: 'app-call',
  templateUrl: './call.component.html',
  styleUrls: ['./call.component.css']
})
export class CallComponent implements OnInit {

  public session$: Observable<SessionDto>=new Observable<SessionDto>();
  public chatSession$: Observable<SessionDto>|null=null;
  public videoSession$: Observable<SessionDto>|null=null;
  public arSession$: Observable<SessionDto>|null=null;
  constructor(private activatedRoute: ActivatedRoute,private sessionFacade:SessionFacade,private chatFacade:ChatFacade,private videoFacade:VideoFacade,private arFacade:ArFacade) {

  }

  ngOnInit(): void {
    this.activatedRoute.params.subscribe(params => {
      let sessionId = params['id'];
      if (sessionId){
        this.session$ = this.sessionFacade.session$(sessionId);
        this.chatSession$ = this.chatFacade.session$(sessionId);
        this.videoSession$ = this.videoFacade.session$(sessionId);
        this.arSession$ = this.arFacade.session$(sessionId);
      }

    });
  }

}

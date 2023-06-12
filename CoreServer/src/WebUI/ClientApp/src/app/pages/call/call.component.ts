import {Component, OnInit} from '@angular/core';
import {Observable} from "rxjs";
import {AppUserDto, CreateSessionCommand, SessionDto} from "../../web-api-client";
import {ActivatedRoute, Router} from "@angular/router";
import {SessionFacade} from "../../services/session-facade.service";
import {ChatFacade} from "../../services/chat-facade.service";
import {VideoFacade} from "../../services/video-facade.service";
import {ArFacade} from "../../services/ar-facade.service";
import {AbstractControl, FormBuilder, ValidationErrors} from "@angular/forms";


@Component({
  selector: 'app-call',
  templateUrl: './call.component.html',
  styleUrls: ['./call.component.css']
})
export class CallComponent implements OnInit {

  public session$: Observable<SessionDto> = new Observable<SessionDto>();
  public chatSession$: Observable<SessionDto> | null = null;
  public videoSession$: Observable<SessionDto> | null = null;
  public arSession$: Observable<SessionDto> | null = null;
  public joinVideo=false;
  public joinAr=false;
  constructor(private activatedRoute: ActivatedRoute,private router:Router, private sessionFacade: SessionFacade, private chatFacade: ChatFacade, private videoFacade: VideoFacade, private arFacade: ArFacade, private fb: FormBuilder) {

  }

  public form = this.fb.group({
    includeChat: [true],
    includeVideo: [true],
    includeAr: [false],
    members: [[] as AppUserDto[], this.arrayRequired]
  });

  ngOnInit(): void {
    this.activatedRoute.params.subscribe(params => {
      let sessionId = params['id'];
      if (sessionId) {
        this.session$ = this.sessionFacade.session$(sessionId);
        this.chatSession$ = this.chatFacade.session$(sessionId);
        this.videoSession$ = this.videoFacade.session$(sessionId);
        this.arSession$ = this.arFacade.session$(sessionId);
      }
    });
    //if qp video=true then join video
    this.joinVideo=!!this.activatedRoute.snapshot.queryParams["video"];
    this.joinAr=!!this.activatedRoute.snapshot.queryParams["ar"];
  }

  public setMembers(members: AppUserDto[]) {
    this.form.controls.members.setValue(members);
  }

  //custom validator that checks if a array is not empty
  public arrayRequired(control: AbstractControl): ValidationErrors | null {
    return control.value.length > 0 ? null : {required: true};
  }

  createSession() {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      return;
    }
    let members = this.form.controls.members.value as AppUserDto[];
    this.sessionFacade.createSession(new CreateSessionCommand({userIds: members.map(u => u.id),name:`Call ${new Date().toLocaleString()} `})).subscribe(session => {
      if (this.form.controls.includeChat.value) {
        this.chatFacade.createChatSession(session.id);
      }
      if (this.form.controls.includeVideo.value) {
        this.videoFacade.createVideoSession(session.id);
      }
      if (this.form.controls.includeAr.value) {
        this.arFacade.createArSession(session.id);
      }
      this.router.navigate([session.id],{relativeTo:this.activatedRoute});
    })
  }
}

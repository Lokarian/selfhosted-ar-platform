import {Component, Input, OnInit} from '@angular/core';
import {ArSessionDto, ChatSessionDto, SessionDto, VideoSessionDto} from "../../web-api-client";
import {VideoFacade} from "../../services/video-facade.service";
import {Observable} from "rxjs";
import {ArFacade} from "../../services/ar-facade.service";
import {ChatFacade} from "../../services/chat-facade.service";
import {CurrentUserService} from "../../services/user/current-user.service";
import {DomSanitizer} from "@angular/platform-browser";
import {AuthorizeService} from "../../services/auth/authorize.service";

@Component({
  selector: 'app-multi-session',
  templateUrl: './multi-session.component.html',
  styleUrls: ['./multi-session.component.css']
})
export class MultiSessionComponent implements OnInit {
  @Input() baseSession: SessionDto = null;
  @Input() initiallyShowChat: boolean = false;
  @Input() initiallyShowVideo: boolean = false;
  @Input() initiallyShowAr: boolean = false;

  public chatSession$ = new Observable<ChatSessionDto>();
  public videoSession$ = new Observable<VideoSessionDto>();
  public arSession$ = new Observable<ArSessionDto>();

  public showChat: boolean;
  public showVideo: boolean;
  public showAr: boolean;

  public joinedVideoSession: boolean = false;
  public joinedArSession: boolean = false;

  constructor(private chatFacade: ChatFacade, private videoFacade: VideoFacade, private arFacade: ArFacade, private domsanitizer: DomSanitizer, private authorizeService: AuthorizeService) {
  }

  ngOnInit(): void {
    this.chatSession$ = this.chatFacade.session$(this.baseSession.id);
    this.videoSession$ = this.videoFacade.session$(this.baseSession.id);
    this.arSession$ = this.arFacade.session$(this.baseSession.id);
    this.showChat = this.initiallyShowChat;
    this.showVideo = this.initiallyShowVideo;
    this.showAr = this.initiallyShowAr;
    this.joinedVideoSession = this.initiallyShowVideo;

  }

  joinArSession(asHololens: boolean) {
    /*
        (navigator as any).userAgentData.getHighEntropyValues(["platformVersion"])
            .then(ua => {
              if ((navigator as any).userAgentData.platform === "Windows") {
                const majorPlatformVersion = parseInt(ua.platformVersion.split('.')[0]);
                if (majorPlatformVersion == 12) {
                  this.authorizeService.getAccessToken().subscribe(token=>{
                    window.open(`arplatform://${location.host}/${this.baseSession.id}?token=${token}`, "_blank");
                  });
                  return;
                }
              }
              this.joinedArSession=true;
            });
    */

    if (asHololens) {
      this.authorizeService.getAccessToken().subscribe(token => {
        window.open(`arplatform://${location.host}/${this.baseSession.id}?token=${token}`, "_blank");
      });
      return;
    } else {
      this.joinedArSession = true;
    }

  }
}

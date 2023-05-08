import {Component, Input, OnInit} from '@angular/core';
import {JoinVideoSessionCommand, VideoClient, VideoSessionDto} from "../../web-api-client";
import {VideoFacade} from "../../services/video-facade.service";
import {Router} from "@angular/router";

@Component({
  selector: 'app-video-session-preview',
  templateUrl: './video-session-preview.component.html',
  styleUrls: ['./video-session-preview.component.css']
})
export class VideoSessionPreviewComponent implements OnInit {

  @Input() session: VideoSessionDto;

  constructor(private videoFacade: VideoFacade, private router: Router) {
  }

  ngOnInit(): void {
  }

  public joinSession() {
    this.videoFacade.joinVideoSession(this.session.baseSessionId);
  }

  joinCall() {
    this.videoFacade.joinVideoSession(this.session.baseSessionId).subscribe(_=>{
      this.router.navigate(['/video', this.session.baseSessionId]);
    })
  }
}

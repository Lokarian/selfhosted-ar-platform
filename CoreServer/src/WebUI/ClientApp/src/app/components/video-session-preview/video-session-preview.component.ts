import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
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
  @Output() join:EventEmitter<VideoSessionDto>=new EventEmitter();
  constructor(private videoFacade: VideoFacade, private router: Router) {
  }

  ngOnInit(): void {
  }

  joinCall() {
    this.join.emit();
  }
}

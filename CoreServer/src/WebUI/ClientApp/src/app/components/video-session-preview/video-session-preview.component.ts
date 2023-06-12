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
  @Input() big:boolean=false;
  @Output() join:EventEmitter<VideoSessionDto>=new EventEmitter();
  constructor() {
  }

  ngOnInit(): void {
  }

  joinCall() {
    this.join.emit(this.session);
  }
}

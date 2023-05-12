import {AfterViewInit, Component, ElementRef, Input, OnInit, ViewChild} from '@angular/core';
import {AppUserDto, VideoStreamDto} from "../../web-api-client";

@Component({
  selector: 'app-video-stream',
  templateUrl: './video-stream.component.html',
  styleUrls: ['./video-stream.component.css']
})
export class VideoStreamComponent implements OnInit,AfterViewInit {
  @Input() stream: MediaStream;
  @Input() user?: AppUserDto;
  @Input() muted: boolean = false;
  @ViewChild('video') video:ElementRef;


  constructor() { }
  ngOnInit() {
  }

  ngAfterViewInit(): void {

  }

  get hasVideo(){
    return this.stream.getVideoTracks().length > 0;
  }
}

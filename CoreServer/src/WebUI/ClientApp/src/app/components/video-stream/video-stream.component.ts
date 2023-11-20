import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  ViewChild,
  ViewEncapsulation
} from '@angular/core';
import {AppUserDto, VideoStreamDto} from "../../web-api-client";

@Component({
  selector: 'app-video-stream',
  templateUrl: './video-stream.component.html',
  styleUrls: ['./video-stream.component.css']
})
export class VideoStreamComponent implements OnInit,AfterViewInit {
  @Input() stream: MediaStream;
  @Input() userId?: string;
  @Input() muted: boolean = false;
  @Output() clicked: EventEmitter<MediaStream> = new EventEmitter<MediaStream>();
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

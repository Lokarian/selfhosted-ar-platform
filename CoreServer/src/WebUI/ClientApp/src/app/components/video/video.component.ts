import {ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {Subject} from "@microsoft/signalr";
import {BehaviorSubject, Observable} from "rxjs";
import {map} from "rxjs/operators";
import {FormBuilder, FormControl, FormGroup, NgForm, Validators} from "@angular/forms";

@Component({
  selector: 'app-video',
  templateUrl: './video.component.html',
  styleUrls: ['./video.component.css']
})
export class VideoComponent implements OnInit, OnDestroy {
  public connected = false;
  public cameraEnabled = false;
  public streaming = false;
  public streamKey: string | null = null;
  public shoutOut = 'you';
  private inputStream?: MediaStream;
  private mediaRecorder?: MediaRecorder;
  private requestAnimation?: number;
  private nameRef = '';
  private blobSubject = new Subject<Blob>();

  @ViewChild('videoElement') videoElement?: ElementRef<HTMLVideoElement>;
  @ViewChild('canvasElement') canvasElement?: ElementRef<HTMLCanvasElement>;

  private availableMediaDevicesSubject = new BehaviorSubject<MediaDeviceInfo[]>([]);
  public availableCameras$ = this.availableMediaDevicesSubject.pipe(map(devices => devices.filter(d => d.kind === 'videoinput')));
  public availableMicrophones$ = this.availableMediaDevicesSubject.pipe(map(devices => devices.filter(d => d.kind === 'audioinput')));

  public otherUserStreams$ = new BehaviorSubject<MediaStream[]>([]);
  public audioEnabled = true;
  public videoEnabled = true;


  constructor(private formBuilder: FormBuilder, private cdr: ChangeDetectorRef) {
  }

  ngOnInit(): void {
    this.shoutOut = 'you';
    this.getMediaDevices();
  }

  ngOnDestroy(): void {
    if (this.requestAnimation) {
      cancelAnimationFrame(this.requestAnimation);
    }
  }

  stopStreaming() {
    this.streaming = false;
    this.mediaRecorder?.stop();
  }

  public getMediaDevices() {
    navigator.mediaDevices.enumerateDevices().then(devices => {
      this.availableMediaDevicesSubject.next(devices);
      this.cdr.detectChanges();
    });
  }

  async enableCamera({video, audio}: { video: MediaDeviceInfo, audio: MediaDeviceInfo }) {
    const constraint: MediaStreamConstraints = {
      video: {
        deviceId: video?.deviceId
      },
      audio: {
        deviceId: audio?.deviceId
      }
    }
    const stream = await navigator.mediaDevices.getUserMedia(constraint);
    this.inputStream = stream;

    if (this.videoElement) {
      this.videoElement.nativeElement.srcObject = stream;
      await this.videoElement.nativeElement.play();

      // We need to set the canvas height/width to match the video element.
      if (this.canvasElement) {
        this.canvasElement.nativeElement.height = this.videoElement.nativeElement.clientHeight;
        this.canvasElement.nativeElement.width = this.videoElement.nativeElement.clientWidth;
      }

      this.requestAnimation = requestAnimationFrame(this.updateCanvas.bind(this));
      this.cameraEnabled = true;
    }
  }

  updateCanvas() {
    if (this.videoElement.nativeElement.ended || this.videoElement.nativeElement.paused) {
      return;
    }
    const ctx = this.canvasElement.nativeElement.getContext("2d");
    ctx.drawImage(
      this.videoElement.nativeElement,
      0,
      0,
      this.canvasElement.nativeElement.clientWidth,
      this.canvasElement.nativeElement.clientHeight
    );
    this.requestAnimation = requestAnimationFrame(this.updateCanvas.bind(this));
  }

  startStreaming() {
    this.streaming = true;
    const videoOutputStream = this.canvasElement?.nativeElement.captureStream(30);
    const audioStream = new MediaStream();
    this.inputStream.getAudioTracks().forEach((track) => {
      audioStream.addTrack(track);
    });
    const outputStream = new MediaStream();
    [audioStream, videoOutputStream].forEach((stream) => {
      stream.getTracks().forEach((track) => {
        outputStream.addTrack(track);
      });
    });
    this.mediaRecorder = new MediaRecorder(outputStream, {
        mimeType: 'video/webm',
        videoBitsPerSecond: 3000000
      }
    );
    this.mediaRecorder.addEventListener('dataavailable', (event) => {
      this.blobSubject.next(event.data);
    });
    this.mediaRecorder.addEventListener('stop', () => {
      this.stopStreaming();
      this.blobSubject.complete();
    });
    this.mediaRecorder.start(1000);
  }
}


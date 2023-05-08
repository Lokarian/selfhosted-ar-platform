import {ChangeDetectorRef, Component, ElementRef, Input, OnInit, ViewChild} from '@angular/core';
import {BehaviorSubject, Observable, ReplaySubject, switchMap} from "rxjs";
import {map} from "rxjs/operators";
import {FormBuilder} from "@angular/forms";
import {NgxPopperjsDirective, NgxPopperjsPlacements, NgxPopperjsTriggers} from 'ngx-popperjs';
import {NotificationService} from "../../services/notification.service";
import {RpcVideoService} from "../../services/rpc/rpc-video.service";
import {ActivatedRoute} from "@angular/router";
import {VideoMemberDto, VideoSessionDto, VideoStreamDto} from "../../web-api-client";
import {VideoFacade} from "../../services/video-facade.service";
import {CurrentUserService} from "../../services/user/current-user.service";
import {DomSanitizer} from "@angular/platform-browser";

@Component({
  selector: 'app-video',
  templateUrl: './video.component.html',
  styleUrls: ['./video.component.css']
})
export class VideoComponent implements OnInit {


  private videoSession: VideoSessionDto;
  public videoStreams: VideoStreamDto[] = [];
  private myMember: VideoMemberDto;

  @Input("session") set sessionIn(value: VideoSessionDto) {
    this.sessionIdReplaySubject.next(value.baseSessionId);
  }

  private sessionIdReplaySubject = new ReplaySubject<string>(1);

  @ViewChild('videoElement') videoElement?: ElementRef<HTMLVideoElement>;
  @ViewChild('videoElement2') videoElement2?: ElementRef<HTMLVideoElement>;
  @ViewChild('canvasElement') canvasElement?: ElementRef<HTMLCanvasElement>;
  @ViewChild('cameraButton', {read: NgxPopperjsDirective}) cameraButtonElement: NgxPopperjsDirective
  @ViewChild('microphoneButton', {read: NgxPopperjsDirective}) microphoneButtonElement: NgxPopperjsDirective

  private availableMediaDevicesSubject = new BehaviorSubject<MediaDeviceInfo[]>([]);
  public availableCameras$ = this.availableMediaDevicesSubject.pipe(map(devices => devices.filter(d => d.kind === 'videoinput')));
  public availableMicrophones$ = this.availableMediaDevicesSubject.pipe(map(devices => devices.filter(d => d.kind === 'audioinput')));

  public selectedCamera?: MediaDeviceInfo;
  public selectedMicrophone?: MediaDeviceInfo;
  public cameraEnabled = false;
  public microphoneEnabled = false;

  private mediaRecorder: MediaRecorder;
  private outputStream: MediaStream;
  private blobSubject: ReplaySubject<Blob> | null = null;

  private microphoneStream: MediaStream | null = null;
  private cameraStream: MediaStream | null = null;
  public enableFrameDrop: Boolean;

  constructor(private formBuilder: FormBuilder, private sanitizer: DomSanitizer, private currentUserService: CurrentUserService, private videoFacade: VideoFacade, private activatedRoute: ActivatedRoute, private rpcVideoService: RpcVideoService, private cdr: ChangeDetectorRef, private notificationService: NotificationService) {
  }

  ngOnInit(): void {
    //initialy load available cameras and microphones
    //check there is an id in the url
    this.activatedRoute.params.subscribe(params => {
      this.sessionIdReplaySubject.next(params.id);
    });
    //listen for session changes

    this.getMediaDevices().then(_ => {
      this.sessionIdReplaySubject
        .pipe(switchMap(sessionId =>
          this.videoFacade.session$(sessionId))
        )
        .subscribe(session => {
          this.onSessionChange(session);
        });
    });

  }


  onSessionChange(session: VideoSessionDto) {
    const oldSession = this.videoSession;
    if (!oldSession || oldSession.baseSessionId !== session.baseSessionId) {
      this.videoSession = session;
      //changed video session, startup process
      this.videoFacade.joinVideoSession(session.baseSessionId).subscribe((tuple) => {
        this.myMember = tuple.item1;
        // create a output stream
        this.createOutputStream().then(async () => {
          //enable camera and microphone
          await this.setCameraStream();
          await this.setMicrophoneStream();
          //request a video stream from the server
          this.videoFacade.requestVideoStream(this.myMember.id).subscribe(stream => {
            //send the output stream to the requested stream
            this.startStreaming(stream);
          })
        });
      });
    }
    //get removed and added streams by dateStopped property
    console.log(session.streams)
    const newActiveStreams = session.streams.filter(s => !s.stoppedAt);
    this.videoStreams = newActiveStreams;


  }

  toggleCamera() {
    if (this.cameraEnabled) {
      this.removeCameraStream();
    } else {
      if (this.selectedCamera) {
        this.setCameraStream();
      } else {
        this.openCameraSelection();
      }
    }
  }

  toggleMicrophone() {
    if (this.microphoneEnabled) {
      this.removeMicrophoneStream();
    } else {
      if (this.selectedMicrophone) {
        this.setMicrophoneStream();
      } else {
        this.openMicrophoneSelection();
      }
    }
  }

  setCamera(device: MediaDeviceInfo) {
    this.selectedCamera = device;
    localStorage.setItem("defaultCamera", device.deviceId);
    if (this.cameraEnabled) {
      this.setCameraStream();
    }
  }

  setMicrophone(device: MediaDeviceInfo) {
    this.selectedMicrophone = device;
    localStorage.setItem("defaultMicrophone", device.deviceId);
    if (this.microphoneEnabled) {
      this.setMicrophoneStream();
    }
  }


  openCameraSelection() {
    this.cameraButtonElement.placement = NgxPopperjsPlacements.TOP;
    this.cameraButtonElement.scheduledShow(0);
  }

  openMicrophoneSelection() {
    this.microphoneButtonElement.placement = NgxPopperjsPlacements.TOP;
    this.microphoneButtonElement.scheduledShow(0);
  }


  public getMediaDevices() {
    return navigator.mediaDevices.enumerateDevices().then(devices => {
      console.log(devices);
      this.availableMediaDevicesSubject.next(devices);
      const defaultCamera = devices.find(d => d.deviceId === localStorage.getItem("defaultCamera"));
      const defaultMicrophone = devices.find(d => d.deviceId === localStorage.getItem("defaultMicrophone"));
      if (defaultCamera) {
        this.setCamera(defaultCamera);
      }
      if (defaultMicrophone) {
        this.setMicrophone(defaultMicrophone);
      }
      this.cdr.detectChanges();
    });
  }

  async setMicrophoneStream() {
    if (!this.selectedMicrophone) {
      return;
    }
    this.microphoneStream = await navigator.mediaDevices.getUserMedia({
      video: false,
      audio: {deviceId: this.selectedMicrophone.deviceId}
    });
    if (!this.microphoneStream) {
      this.notificationService.error("No microphone stream");
      return;
    }
    const track = this.microphoneStream.getAudioTracks()[0];
    if (!track) {
      this.notificationService.error("No microphone track");
      return;
    }
    this.outputStream.getAudioTracks().forEach(t => this.outputStream.removeTrack(t));
    this.outputStream.addTrack(track);
    this.microphoneEnabled = true;
  }

  async setCameraStream() {
    if (!this.selectedCamera) {
      return;
    }
    this.cameraStream = await navigator.mediaDevices.getUserMedia({
      video: {deviceId: this.selectedCamera.deviceId,},
      audio: false
    });
    if (!this.cameraStream) {
      this.notificationService.error("No camera stream");
      return;
    }
    const track = this.cameraStream.getVideoTracks()[0];
    if (!track) {
      this.notificationService.error("No camera track");
      return;
    }
    this.outputStream.getVideoTracks().forEach(t => this.outputStream.removeTrack(t));
    this.outputStream.addTrack(track);
    this.cameraEnabled = true;
  }

  removeCameraStream() {
    this.outputStream.getVideoTracks().forEach(t => this.outputStream.removeTrack(t));
    this.cameraEnabled = false;
  }

  removeMicrophoneStream() {
    this.outputStream.getAudioTracks().forEach(t => this.outputStream.removeTrack(t));
    this.microphoneEnabled = false;
  }


  async createOutputStream() {
    //create dummy stream with a silent audio track to pass to the media switcher
    this.outputStream = new MediaStream();
    const audioContext = new AudioContext();
    const destination = audioContext.createMediaStreamDestination();
    this.outputStream.addTrack(destination.stream.getAudioTracks()[0]);
    //bind the stream to my video preview
    this.videoElement.nativeElement.srcObject = this.outputStream;
  }

  startStreaming(stream: VideoStreamDto) {
    console.log("start streaming");
    this.blobSubject = new ReplaySubject(1);
    this.createMediaRecorder();
    this.rpcVideoService.SendVideoStream(this.blobSubject.asObservable(), stream.id, this.videoFacade.getAccessKey(this.videoSession.baseSessionId));
  }

  createMediaRecorder() {
    console.log(this.outputStream.getTracks())
    this.mediaRecorder = new MediaRecorder(this.outputStream, {
        mimeType: 'video/webm',
        videoBitsPerSecond: 10000000,
      }
    );
    this.mediaRecorder.addEventListener('error', () => {
      this.notificationService.error("media recorder error,retry");
      this.createMediaRecorder();
    });
    this.mediaRecorder.addEventListener('stop', () => {
      this.notificationService.error("media recorder stopped");
    });
    this.mediaRecorder.addEventListener("dataavailable", (event) => {
      console.log("data available");
      //have a 10% chance to drop the frame
      if (Math.random() < 0.1 && this.enableFrameDrop) {
        return;
      }
      this.blobSubject.next(event.data);
    });
    console.log("start media recorder");
    this.mediaRecorder.start(100);
  }

  displayBlobStream(blobs: Observable<Uint8Array>) {
    const mediaSource = new MediaSource();
    this.videoElement2.nativeElement.src = URL.createObjectURL(mediaSource);
    let sourceBuffer: SourceBuffer;
    for (let i = 0; i < mediaSource.activeSourceBuffers.length; i++) {
      let buffer = mediaSource.activeSourceBuffers[i];
      console.log("before", buffer);
    }
    mediaSource.addEventListener('sourceopen', () => {
      for (let i = 0; i < mediaSource.activeSourceBuffers.length; i++) {
        let buffer = mediaSource.activeSourceBuffers[i];
        console.log("before2", buffer);
      }
      sourceBuffer = mediaSource.addSourceBuffer('video/webm; codecs="vp8,opus"');
      blobs.subscribe(blob => {
        for (let i = 0; i < mediaSource.activeSourceBuffers.length; i++) {
          let buffer = mediaSource.activeSourceBuffers[i];
          console.log("during", buffer);
        }
        sourceBuffer.appendBuffer(blob);
      });
    });
  }

  public getStreamUrl(stream: VideoStreamDto) {
    // in form http://localhost:8889/{{userStream.id}}/
    return this.sanitizer.bypassSecurityTrustResourceUrl(`http://localhost:8889/${stream.id}/`);
  }

  stopStreaming() {
    this.mediaRecorder?.removeAllListeners("dataavailable");
    this.blobSubject.complete();
    this.blobSubject = null;
  }

  public NgxPopperjsTriggers = NgxPopperjsTriggers;

  getStream(ev: EventTarget) {
    const id = (ev as HTMLInputElement).value;
    const stream = this.rpcVideoService.getVideoStream(id);
    this.displayBlobStream(stream);
  }
}

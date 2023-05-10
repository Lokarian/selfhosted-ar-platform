import {AfterViewInit, ChangeDetectorRef, Component, ElementRef, Input, OnInit, ViewChild} from '@angular/core';
import {BehaviorSubject, distinct, Observable, ReplaySubject, Subscription, switchMap, tap} from "rxjs";
import {map} from "rxjs/operators";
import {FormBuilder} from "@angular/forms";
import {NgxPopperjsDirective, NgxPopperjsPlacements, NgxPopperjsTriggers} from 'ngx-popperjs';
import {NotificationService} from "../../services/notification.service";
import {RpcVideoService} from "../../services/rpc/rpc-video.service";
import {ActivatedRoute} from "@angular/router";
import {
  VideoClient,
  VideoMemberDto,
  VideoSessionDto,
  VideoStreamDto
} from "../../web-api-client";
import {VideoFacade} from "../../services/video-facade.service";
import {CurrentUserService} from "../../services/user/current-user.service";
import {DomSanitizer} from "@angular/platform-browser";
import {SignalRService, TopicProxy} from "../../services/signalr.service";

interface WebRtcExchangeObject {
  senderId: string,
  receiverId: string,
  description?: RTCSessionDescriptionInit,
  candidate?: RTCIceCandidateInit
}

@Component({
  selector: 'app-video', templateUrl: './video.component.html', styleUrls: ['./video.component.css']
})
export class VideoComponent implements OnInit, AfterViewInit {


  public videoStreams: VideoStreamDto[] = [];
  @ViewChild('videoElement') videoElement?: ElementRef<HTMLVideoElement>;
  @ViewChild('videoElement2') videoElement2?: ElementRef<HTMLVideoElement>;
  @ViewChild('canvasElement') canvasElement?: ElementRef<HTMLCanvasElement>;
  @ViewChild('cameraButton', {read: NgxPopperjsDirective}) cameraButtonElement: NgxPopperjsDirective
  @ViewChild('microphoneButton', {read: NgxPopperjsDirective}) microphoneButtonElement: NgxPopperjsDirective
  public NgxPopperjsTriggers = NgxPopperjsTriggers;
  public selectedCamera?: MediaDeviceInfo;
  public selectedMicrophone?: MediaDeviceInfo;
  public cameraEnabled = false;
  public microphoneEnabled = false;
  private webRtcConnectionDetails: {
    [memberId: string]: {
      pc: RTCPeerConnection,
      amIPolite: boolean,
      ignoreOffer: boolean,
      makingOffer: boolean
    }
  } = {};
  private videoSession: VideoSessionDto;
  public myMember: VideoMemberDto;
  private sessionIdReplaySubject = new ReplaySubject<string>(1);
  private availableMediaDevicesSubject = new BehaviorSubject<MediaDeviceInfo[]>([]);
  public availableCameras$ = this.availableMediaDevicesSubject.pipe(map(devices => devices.filter(d => d.kind === 'videoinput')));
  public availableMicrophones$ = this.availableMediaDevicesSubject.pipe(map(devices => devices.filter(d => d.kind === 'audioinput')));
  private mediaRecorder: MediaRecorder;
  private outputStream: MediaStream;
  private blobSubject: ReplaySubject<Blob> | null = null;
  private microphoneStream: MediaStream | null = null;
  private cameraStream: MediaStream | null = null;
  private signalingChannel: TopicProxy<WebRtcExchangeObject | { joinId: string }>;
  private signalingSubscription: Subscription;

  constructor(private formBuilder: FormBuilder,
              private sanitizer: DomSanitizer,
              private currentUserService: CurrentUserService,
              private videoClient: VideoClient,
              private videoFacade: VideoFacade,
              private activatedRoute: ActivatedRoute,
              private rpcVideoService: RpcVideoService,
              private cdr: ChangeDetectorRef,
              private notificationService: NotificationService,
              private signalRService: SignalRService) {
  }

  @Input("session") set sessionIn(value: VideoSessionDto) {
    this.sessionIdReplaySubject.next(value.baseSessionId);
  }

  ngOnInit(): void {
    //initially load available cameras and microphones
    //check there is an id in the url
    this.activatedRoute.params.subscribe(params => {
      this.sessionIdReplaySubject.next(params.id);
    });
    this.getMediaDevices();
  }

  ngAfterViewInit(): void {
    this.sessionIdReplaySubject.pipe(
      distinct(),
      switchMap(sessionId => this.videoFacade.joinVideoSession(sessionId)),
      tap(tuple => this.myMember = tuple.item1),
      switchMap(tuple => this.videoFacade.session$(tuple.item1.sessionId)),
      tap(session => this.onSessionChange(session)),

    ).subscribe();


  }

  async onSessionChange(session: VideoSessionDto) {
    const oldSession = this.videoSession;
    const isNewSession = !this.videoSession || this.videoSession.baseSessionId !== session.baseSessionId;
    this.videoSession = session;
    //if the session changed, create an output stream and register for WebRtcOffers from facade
    if (isNewSession) {
      this.createOutputStream();
      this.joinSignalingSession();
      for (const member of session.members.filter(m => m.id !== this.myMember.id)) {
        //open connections to all already connected members except myself. existing users are always impolite
        await this.startWebRtcConnection(member.id, true);
      }
      this.signalingChannel.subject.next({joinId: this.myMember.id})
    } else {
      /*//get new members
      const newMembers = session.members.filter(m => !oldSession.members.some(m2 => m2.id === m.id));
      for (const member of newMembers) {
        await this.onNewMember(member);
      }
      //get removed members
      const removedMembers = oldSession.members.filter(m => !session.members.some(m2 => m2.id === m.id));
      for (const member of removedMembers) {
        //todo: close connection
        console.log("removing member", member.id);
      }*/
    }

  }


  async onNewMember(memberId: string) {
    await this.startWebRtcConnection(memberId, false)
  }

  async startWebRtcConnection(memberId: string, amIPolite: boolean) {
    console.log("starting webrtc connection to", memberId);
    const configuration = {'iceServers': [{'urls': 'stun:stun.l.google.com:19302'}]}
    const pc = new RTCPeerConnection(configuration);
    this.webRtcConnectionDetails[memberId] = {
      pc: pc,
      amIPolite: amIPolite,
      makingOffer: false,
      ignoreOffer: false,
    };
    try {
      const stream = await navigator.mediaDevices.getUserMedia({video: true, audio: true});
      //const stream = this.outputStream;
      console.assert(!!stream);
      for (const track of stream.getTracks()) {
        pc.addTrack(track, stream);
      }
    } catch (err) {
      console.error(err);
    }
    pc.ontrack = ({track, streams}) => {
      console.log("received track", track, streams);
      track.onunmute = () => {
        console.log("unmuted track", track,streams);
        this.videoElement2.nativeElement.srcObject = streams[0];

      };
    };
    let makingOffer = false;

    pc.onnegotiationneeded = async () => {
      console.log("negotiation needed");
      try {
        makingOffer = true;
        await pc.setLocalDescription();
        console.log("sending offer", pc.localDescription.toJSON());
        this.signalingChannel.subject.next({
          description: pc.localDescription.toJSON(),
          receiverId: memberId,
          senderId: this.myMember.id
        });
      } catch (err) {
        console.error(err);
      } finally {
        makingOffer = false;
      }
    };
    pc.onicecandidate = ({candidate}) => {
      console.log("sending ice");
      this.signalingChannel.subject.next({
        candidate: candidate===null?null:candidate.toJSON(),
        receiverId: memberId,
        senderId: this.myMember.id
      });
    }
    pc.onconnectionstatechange = (event) => {
      console.log("CONNECTION state changed", (event.target as any).connectionState);
    }

  }

  joinSignalingSession() {
    if (this.signalingChannel) {
      this.signalingChannel.subject.complete();
    }
    if(this.signalingSubscription){
      this.signalingSubscription.unsubscribe();
    }
    this.signalingChannel = this.signalRService.topicProxy<WebRtcExchangeObject | { joinId: string }>(this.videoSession.baseSessionId);
    this.signalingSubscription= this.signalingChannel.stream.subscribe(data=>{
      //if else path type
      if((data as any).joinId){
        if((data as any).joinId!==this.myMember.id){
          console.log("new user in session", data);
          this.onNewMember((data as any).joinId);
        }
      }
      else{
        const offer = data as WebRtcExchangeObject;
        if(offer.receiverId===this.myMember.id){
          this.onWebRtcOffer(offer);
        }
      }
    })
  }

  async onWebRtcOffer(offer: WebRtcExchangeObject) {
    const connectionDetails = this.webRtcConnectionDetails[offer.senderId];
    const {description, candidate} = offer
    try {
      if (description) {
        console.log("received description", description);
        const offerCollision =
          description.type === "offer" &&
          (connectionDetails.makingOffer || connectionDetails.pc.signalingState !== "stable");
        connectionDetails.ignoreOffer = !connectionDetails.amIPolite && offerCollision;
        if (connectionDetails.ignoreOffer) {
          return;
        }

        await connectionDetails.pc.setRemoteDescription(description);
        if (description.type === "offer") {
          await connectionDetails.pc.setLocalDescription();
          //send offer in signaling channel
          console.log("sending answer", connectionDetails.pc.localDescription.toJSON());
          this.signalingChannel.subject.next({
            description: connectionDetails.pc.localDescription.toJSON(),
            receiverId: offer.senderId,
            senderId: this.myMember.id
          });

        }
      } else if (candidate!==undefined) {
        console.log("received candidate");
        try {
          await connectionDetails.pc.addIceCandidate(candidate);
        } catch (err) {
          if (!connectionDetails.ignoreOffer) {
            throw err;
          }
        }
      }
    } catch (err) {
      console.error(err);
    }
  }

  /**
   * query the available media devices, instantly enable if default device is set
   */
  public getMediaDevices() {
    return navigator.mediaDevices.enumerateDevices().then(devices => {
      this.availableMediaDevicesSubject.next(devices);
      const defaultCamera = devices.find(d => d.deviceId === localStorage.getItem("defaultCamera"));
      const defaultMicrophone = devices.find(d => d.deviceId === localStorage.getItem("defaultMicrophone"));
      if (defaultCamera) {
        this.selectCamera(defaultCamera);
      }
      if (defaultMicrophone) {
        this.selectMicrophone(defaultMicrophone);
      }
      this.cdr.detectChanges();
    });
  }

  createOutputStream() {
    //create dummy stream with a silent audio track to pass to the media switcher
    this.outputStream = new MediaStream();
    const audioContext = new AudioContext();
    const destination = audioContext.createMediaStreamDestination();
    this.outputStream.addTrack(destination.stream.getAudioTracks()[0]);
    //bind the stream to my video preview
    this.videoElement.nativeElement.srcObject = this.outputStream;
  }


  selectCamera(device: MediaDeviceInfo) {
    this.selectedCamera = device;
    localStorage.setItem("defaultCamera", device.deviceId);
    if (this.cameraEnabled) {
      this.setCameraStream();
    }
  }

  selectMicrophone(device: MediaDeviceInfo) {
    this.selectedMicrophone = device;
    localStorage.setItem("defaultMicrophone", device.deviceId);
    if (this.microphoneEnabled) {
      this.setMicrophoneStream();
    }
  }

  async setMicrophoneStream() {
    if (!this.selectedMicrophone) {
      return;
    }
    this.microphoneStream = await navigator.mediaDevices.getUserMedia({
      video: false, audio: {deviceId: this.selectedMicrophone.deviceId}
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
      video: {deviceId: this.selectedCamera.deviceId,}, audio: false
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
    for(let [key,val] of Object.entries(this.webRtcConnectionDetails)){
      val.pc.addTrack(track,this.outputStream);
    }

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

  openCameraSelection() {
    this.cameraButtonElement.placement = NgxPopperjsPlacements.TOP;
    this.cameraButtonElement.scheduledShow(0);
  }

  openMicrophoneSelection() {
    this.microphoneButtonElement.placement = NgxPopperjsPlacements.TOP;
    this.microphoneButtonElement.scheduledShow(0);
  }

}

/*

class MediaMtxStreaming {
  constructor(private mediaRecorder: MediaRecorder, private blobSubject: ReplaySubject<Uint8Array>, private myMember: VideoMemberDto, private videoFacade: VideoFacade) {
  }

  stopStreaming() {
    this.mediaRecorder?.removeAllListeners("dataavailable");
    this.blobSubject.complete();
    this.blobSubject = null;
  }

  startMediaMtxStreaming() {
    //request a stream and start streaming
    this.videoFacade.requestVideoStream(this.myMember.id).subscribe(stream => {
      this.startStreaming(stream);
    })
  }

  createMediaRecorder() {
    console.log(this.outputStream.getTracks())
    this.mediaRecorder = new MediaRecorder(this.outputStream, {
      mimeType: 'video/webm', videoBitsPerSecond: 10000000,
    });
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

  public getStreamUrl(stream: VideoStreamDto) {
    // in form http://localhost:8889/{{userStream.id}}/
    return this.sanitizer.bypassSecurityTrustResourceUrl(`http://localhost:8889/${stream.id}/`);
  }

  startStreaming(stream: VideoStreamDto) {
    console.log("start streaming");
    this.blobSubject = new ReplaySubject(1);
    this.createMediaRecorder();
    this.rpcVideoService.SendVideoStream(this.blobSubject.asObservable(), stream.id, this.videoFacade.getAccessKey(this.videoSession.baseSessionId));
  }
}

class ManualBlobStream {

  constructor(private videoElement2: ElementRef<HTMLVideoElement>) {

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

}
*/

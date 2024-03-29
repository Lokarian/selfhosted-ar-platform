import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ComponentRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  ViewChild,
  ViewContainerRef,
  ViewEncapsulation
} from '@angular/core';
import {LeaveVideoSessionCommand, VideoClient, VideoMemberDto, VideoSessionDto} from "../../web-api-client";
import {BehaviorSubject, concatMap, distinctUntilChanged, switchMap, tap} from "rxjs";
import {filter, map} from "rxjs/operators";
import {NgxPopperjsDirective, NgxPopperjsPlacements, NgxPopperjsTriggers} from 'ngx-popperjs';
import {SignalRService, TopicProxy} from "../../services/signalr.service";
import {VideoFacade} from "../../services/video-facade.service";
import {NotificationService} from "../../services/notification.service";
import {environment} from "../../../environments/environment";
import {VideoStreamComponent} from "../video-stream/video-stream.component";

interface WebRtcExchangeObject {
  senderId: string,
  receiverId: string,
  description?: RTCSessionDescriptionInit,
  candidate?: RTCIceCandidateInit
}

@Component({
  selector: 'app-video-session',
  templateUrl: './video-session.component.html',
  styleUrls: ['./video-session.component.css'],
  encapsulation: ViewEncapsulation.None
})
export class VideoSessionComponent implements OnInit, AfterViewInit {
  private sessionSubject = new BehaviorSubject<VideoSessionDto>(null);
  public session$ = this.sessionSubject.asObservable().pipe(filter(s => !!s));
  private myMemberSubject = new BehaviorSubject<VideoMemberDto>(null);
  public myMember$ = this.myMemberSubject.asObservable().pipe(filter(m => !!m));

  public get myMember() {
    return this.myMemberSubject.value;
  }

  @Input() set session(value: VideoSessionDto) {
    this.sessionSubject.next(value);
  }

  get session() {
    return this.sessionSubject.value;
  }

  @Output() onLeaveCall = new EventEmitter();
  @ViewChild('cameraButton', {read: NgxPopperjsDirective}) cameraButtonElement: NgxPopperjsDirective
  @ViewChild('microphoneButton', {read: NgxPopperjsDirective}) microphoneButtonElement: NgxPopperjsDirective
  public NgxPopperjsTriggers = NgxPopperjsTriggers;
  private availableMediaDevicesSubject = new BehaviorSubject<MediaDeviceInfo[]>([]);
  public availableCameras$ = this.availableMediaDevicesSubject.pipe(map(devices => devices.filter(d => d.kind === 'videoinput')));
  public availableMicrophones$ = this.availableMediaDevicesSubject.pipe(map(devices => devices.filter(d => d.kind === 'audioinput')));


  private webRtcConnectionDetails: Map<string, {
    pc: RTCPeerConnection,
    amIPolite: boolean,
    trackMap: Map<MediaStreamTrack, RTCRtpSender>,
    ignoreOffer: boolean,
    makingOffer: boolean
  }> = new Map();
  private signalingChannel: TopicProxy<WebRtcExchangeObject | { joinId: string }>;

  public remoteStreams: { stream: MediaStream, memberId: string }[] = [];
  public focusedStream: string | null = null;
  public userMediaStream: MediaStream;
  public screenShareStream: MediaStream | undefined;

  //used for setting the stream after clickon on unmute without having a selected device
  private cameraEnabledDesired = false;
  private microphoneEnabledDesired = false;
  public selectedCamera?: MediaDeviceInfo;
  public selectedMicrophone?: MediaDeviceInfo;

  private myTracksForCleanup: MediaStreamTrack[] = [];

  constructor(private videoClient: VideoClient,
              private videoFacade: VideoFacade,
              private notificationService: NotificationService,
              private signalRService: SignalRService,
              private cdr: ChangeDetectorRef) {

  }

  ngOnInit(): void {
  }

  async ngAfterViewInit() {
    this.session$.pipe(
      map(s => s.baseSessionId),
      distinctUntilChanged(),
      switchMap(sessionId => this.videoFacade.joinVideoSession(sessionId)),
      tap(tuple => this.myMemberSubject.next(tuple.item1)),
      map(tuple => this.videoFacade.session(tuple.item1.sessionId)),
      concatMap(async session => this.onNewSession(session)),
      switchMap(session => this.videoFacade.session$(session.baseSessionId)),
      tap(session => this.sessionSubject.next(session)),
      tap(session => this.onSessionDetailChanged(session)),
    ).subscribe();
  }

  async onNewSession(session: VideoSessionDto) {
    await this.createUserMediaStream();
    this.joinSignalingSession();
    console.log("announcing myself");
    this.signalingChannel.subject.next({joinId: this.myMember.id});
    return session
  }

  onSessionDetailChanged(session: VideoSessionDto) {
    console.log("session detail changed current members", session.members.map(m => m.id));
    console.log("current webrtc connections", JSON.stringify(this.webRtcConnectionDetails.keys()));

    this.webRtcConnectionDetails.forEach((value, key) => {
      if (!session.members.some(m => m.id === key)) {
        console.log("member", key, "left the session");
        this.webRtcConnectionDetails.get(key).pc.close();
        for (let val of this.remoteStreams.values()) {
          if (val.memberId === key) {
            this.removeStreamFromUi(val.stream);
          }
        }
        this.webRtcConnectionDetails.delete(key);
      }
    });
  }

  async startWebRtcConnection(memberId: string, amIPolite: boolean) {
    console.log("starting webrtc connection to", memberId, amIPolite ? " politely" : " impolitely");
    const pc = new RTCPeerConnection(environment.iceConfiguration);
    this.webRtcConnectionDetails.set(memberId, {
      pc: pc,
      amIPolite: amIPolite,
      trackMap: new Map<MediaStreamTrack, RTCRtpSender>(),
      makingOffer: false,
      ignoreOffer: false,
    });
    try {
      this.userMediaStream.getTracks().forEach(track => {
        const sender = pc.addTrack(track, this.userMediaStream);
        this.webRtcConnectionDetails.get(memberId).trackMap.set(track, sender);
      });
      this.screenShareStream?.getTracks().forEach(track => {
        const sender = pc.addTrack(track, this.screenShareStream);
        this.webRtcConnectionDetails.get(memberId).trackMap.set(track, sender);
      });
    } catch (err) {
      console.error(err);
    }
    pc.ontrack = (event) => {
      this.onRemoteTrack(event, memberId);
    };
    let makingOffer = false;
    pc.onnegotiationneeded = async () => {
      console.log("negotiation needed");
      try {
        makingOffer = true;
        const offer = await pc.createOffer();
        if (pc.signalingState != "stable") return;
        await pc.setLocalDescription(offer);
        this.signalingChannel.subject.next({
          description: pc.localDescription.toJSON(),
          receiverId: memberId,
          senderId: this.myMember.id
        });
      } catch (e) {
        console.warn(`ONN ${e}`);
      } finally {
        makingOffer = false;
      }
    };
    pc.onicecandidate = ({candidate}) => {
      this.signalingChannel.subject.next({
        candidate: candidate?.toJSON() ?? candidate,
        receiverId: memberId,
        senderId: this.myMember.id
      });
    }
    pc.onconnectionstatechange = (event) => {
      console.log("connection state changed", (event.target as any).connectionState);
      if ((event.target as any).connectionState == "disconnected") {
        this.onDisconnected(memberId);
      }
    };
  }

  async onWebRtcOffer(offer: WebRtcExchangeObject) {
    const connectionDetails = await this.getWebRtcConnection(offer.senderId);
    const {description, candidate} = offer
    if (description) {
      if (description.type == "offer" && connectionDetails.pc.signalingState != "stable") {
        if (!connectionDetails.amIPolite) {
          console.log("ignoring offer");
          return;
        }
        console.log("rolling back local description")
        await Promise.all([
          connectionDetails.pc.setLocalDescription({type: "rollback"}),
          connectionDetails.pc.setRemoteDescription(description)
        ]);
      } else {
        await connectionDetails.pc.setRemoteDescription(description);
      }
      if (description.type == "offer") {
        await connectionDetails.pc.setLocalDescription(await connectionDetails.pc.createAnswer());
        this.signalingChannel.subject.next({
          description: connectionDetails.pc.localDescription.toJSON(),
          receiverId: offer.senderId,
          senderId: this.myMember.id
        })
      }
    } else if (candidate !== undefined) await connectionDetails.pc.addIceCandidate(candidate);
  }

  async getWebRtcConnection(memberId: string) {
    let connectionDetails = this.webRtcConnectionDetails.get(memberId);
    // if we do not have a connection to this member, create one
    if (!connectionDetails) {
      await this.startWebRtcConnection(memberId, true);
      connectionDetails = this.webRtcConnectionDetails.get(memberId);
    }
    return connectionDetails;
  }


  joinSignalingSession() {
    this.signalingChannel = this.signalRService.topicProxy<WebRtcExchangeObject | {
      joinId: string
    }>(this.session.baseSessionId);
    this.signalingChannel.stream.pipe(concatMap(
      async data => {
        if ((data as any).joinId) {
          if ((data as any).joinId !== this.myMember.id) {
            console.log("new user in session", data, "creating new impolite connection");
            await this.onNewMember((data as any).joinId);
          }
        } else {
          const offer = data as WebRtcExchangeObject;
          if (offer.receiverId === this.myMember.id) {
            await this.onWebRtcOffer(offer);
          }
        }
      })).subscribe();
  }

  async onNewMember(memberId: string) {
    await this.startWebRtcConnection(memberId, false);
  }

  onRemoteTrack(event: RTCTrackEvent, memberId: string) {
    console.log("received remote track", event);
    const streams = event.streams;
    if (streams.length == 0) {
      console.warn("no streams in event", event);
      return;
    }
    for (let incoming_stream of streams) {
      let stream = this.remoteStreams.find(s => s.stream.id == incoming_stream.id)?.stream;
      if (!stream) {
        console.log("adding new stream", incoming_stream);
        this.remoteStreams.push({stream: incoming_stream, memberId: memberId});
        incoming_stream.onremovetrack = (event) => {
          console.log("removing remote track", event, incoming_stream);
          //if last track is removed, remove stream
          if (incoming_stream?.getTracks().length == 0) {
            console.log("removing stream", incoming_stream);
            this.remoteStreams = this.remoteStreams.filter(s => s.stream.id != incoming_stream?.id);
            this.removeStreamFromUi(incoming_stream);
          }
        }
        this.addStreamToUi(incoming_stream, memberId);

      }
    }
  }

  onDisconnected(memberId: string) {
    //remove all streams from this member
    this.remoteStreams.filter(s => s.memberId == memberId).forEach(s => {
      this.removeStreamFromUi(s.stream);
    });
    this.remoteStreams = this.remoteStreams.filter(s => s.memberId != memberId);
  }

  @ViewChild("normalListView", {read: ViewContainerRef}) normalListView: ViewContainerRef;
  @ViewChild("focusedListView", {read: ViewContainerRef}) focusedListView: ViewContainerRef;
  @ViewChild("focusedView", {read: ViewContainerRef}) focusedView: ViewContainerRef;
  private mediaStreamComponentMap = new Map<string, ComponentRef<VideoStreamComponent>>();

  removeStreamFromUi(stream: MediaStream) {
    const componentRef = this.mediaStreamComponentMap.get(stream.id);
    if (componentRef) {
      if (stream.id === this.focusedStream) {
        this.focusStream(stream);
      }
      //destroy the component and remove it from the map
      componentRef.destroy();
      this.mediaStreamComponentMap.delete(stream.id);
    }
  }

  addStreamToUi(stream: MediaStream, memberId: string) {
    const viewContainerRef: ViewContainerRef = this.focusedStream ? this.focusedListView : this.normalListView;
    let componentRef = viewContainerRef.createComponent(VideoStreamComponent);
    componentRef.instance.stream = stream;
    componentRef.instance.userId = this.session.members.find(m => m.id == memberId)?.userId;
    componentRef.instance.muted = memberId === this.myMember.id;
    console.log("mute", memberId === this.myMember.id, memberId, this.myMember.id);
    componentRef.instance.clicked.subscribe(() => {
      this.focusStream(stream);
    });
    this.mediaStreamComponentMap.set(stream.id, componentRef);
  }

  focusStream(stream: MediaStream) {
    if (stream.id === this.focusedStream) {
      Array.from(this.mediaStreamComponentMap.values()).forEach(c => {
        //if the element is not inside normalListView, move it there
        if (!this.normalListView.element.nativeElement.contains(c.location.nativeElement)) {
          this.normalListView.insert(c.hostView);
        }
      });
      this.focusedStream = null;
    } else {
      //move all elements that are not inside focusedListView to focusedListView
      Array.from(this.mediaStreamComponentMap.values()).forEach(c => {
        if (!this.focusedListView.element.nativeElement.contains(c.location.nativeElement)) {
          this.focusedListView.insert(c.hostView);
        }
      });
      //move the focused stream to focusedView
      const componentRef = this.mediaStreamComponentMap.get(stream.id);
      if (!componentRef) {
        console.warn("stream not found in map", stream);
        return;
      }
      this.focusedView.insert(componentRef.hostView);
      this.focusedStream = stream.id;
    }
    this.cdr.detectChanges();
  }

  addLocalTrack(track: MediaStreamTrack, stream: MediaStream) {
    console.log("adding local track", track, stream);
    stream.addTrack(track);
    for (let connectionDetails of this.webRtcConnectionDetails.values()) {
      const sender = connectionDetails.pc.addTrack(track, stream);
      connectionDetails.trackMap.set(track, sender);
    }
  }

  removeLocalTrack(track: MediaStreamTrack, stream: MediaStream) {
    console.log("removing local track", track, stream);
    //stop track if it is still active
    if (track.readyState == "live") {
      track.stop();
    }
    stream.removeTrack(track);
    for (let connectionDetails of this.webRtcConnectionDetails.values()) {
      const sender = connectionDetails.trackMap.get(track);
      if (sender) {
        connectionDetails.pc.removeTrack(sender);
      }
    }
  }

  async createUserMediaStream() {
    //create dummy stream with a silent audio track to pass to the media switcher
    this.userMediaStream = new MediaStream();
    const audioContext = new AudioContext();
    const destination = audioContext.createMediaStreamDestination();
    this.addLocalTrack(destination.stream.getAudioTracks()[0], this.userMediaStream);
    this.addStreamToUi(this.userMediaStream, this.myMember.id);
  }

  public async getMediaDevices() {
    //ask for permission to use microphone and camera
    try {
      (await navigator.mediaDevices.getUserMedia({video: true, audio: true})).getTracks().forEach(t => t.stop());
    } catch (e) {
      this.notificationService.error(`You need to allow access to your microphone and camera to use this feature. Please refresh the page and try again.`);
    }
    const devices = await navigator.mediaDevices.enumerateDevices();
    this.availableMediaDevicesSubject.next(devices);
  }

  get cameraEnabled() {
    return this.userMediaStream?.getVideoTracks().length > 0;
  }

  get microphoneEnabled() {
    const audioTracks = this.userMediaStream?.getAudioTracks()
    return audioTracks?.some(t => !t.getSettings().deviceId.startsWith("WebAudio"));
  }

  public toggleMicrophone() {
    if (this.microphoneEnabled) {
      this.userMediaStream.getAudioTracks().forEach(t => this.removeLocalTrack(t, this.userMediaStream));
      //todo add empty audio track
      this.microphoneEnabledDesired = false;
    } else {
      if (this.selectedMicrophone) {
        this.microphoneEnabledDesired = true;
        this.setMicrophone(this.selectedMicrophone);
      } else {
        this.microphoneEnabledDesired = true;
        this.showMicrophonePopper();
      }
    }
  }

  public toggleCamera() {
    if (this.cameraEnabled) {
      this.userMediaStream.getVideoTracks().forEach(t => this.removeLocalTrack(t, this.userMediaStream));
      this.cameraEnabledDesired = false;
    } else {
      if (this.selectedCamera) {
        this.cameraEnabledDesired = true;
        this.setCamera(this.selectedCamera);
      } else {
        this.cameraEnabledDesired = true;
        this.showCameraPopper();
      }
    }
  }

  toggleScreenShare() {
    if (this.screenShareStream) {
      this.stopScreenShare();
    } else {
      navigator.mediaDevices.getDisplayMedia({video: true, audio: true}).then(stream => {
        this.screenShareStream = stream;
        stream.getTracks().forEach(t => this.addLocalTrack(t, this.screenShareStream));
        //listen to outside stream stop
        stream.getVideoTracks()[0].addEventListener('ended', () => {
          this.stopScreenShare();
        });
        this.addStreamToUi(stream, this.myMember.id);
        stream.getTracks().forEach(t => this.myTracksForCleanup.push(t));
      });
    }
  }

  stopScreenShare() {
    // stop the screen share if it is still active
    if (this.screenShareStream) {
      this.screenShareStream.getTracks().forEach(t => t.stop());
    }
    this.screenShareStream.getTracks().forEach(t => this.removeLocalTrack(t, this.screenShareStream));
    this.removeStreamFromUi(this.screenShareStream);
    this.screenShareStream = null;
  }

  public setMicrophone(device: MediaDeviceInfo) {
    this.selectedMicrophone = device;
    if (this.microphoneEnabledDesired) {
      //remove old microphone track if it exists
      this.userMediaStream.getAudioTracks().forEach(t => {
        this.removeLocalTrack(t, this.userMediaStream);
      });
      //add new microphone track
      navigator.mediaDevices.getUserMedia({audio: {deviceId: device.deviceId}}).then(stream => {
        if (stream.getAudioTracks().length !== 1) {
          console.warn("unexpected number of audio tracks when setting microphone", stream.getAudioTracks());
        }
        this.addLocalTrack(stream.getAudioTracks()[0], this.userMediaStream);
        stream.getTracks().forEach(t => this.myTracksForCleanup.push(t));
      });
    }
  }

  public setCamera(device: MediaDeviceInfo) {
    this.selectedCamera = device;
    if (this.cameraEnabledDesired) {
      //remove old camera track if it exists
      this.userMediaStream.getVideoTracks().forEach(t => {
        this.removeLocalTrack(t, this.userMediaStream);
      });
      //add new camera track
      this.getCameraTrack(device).then(stream => {
        if (stream.getVideoTracks().length !== 1) {
          console.warn("unexpected number of video tracks when setting camera", stream.getVideoTracks());
        }
        this.addLocalTrack(stream.getVideoTracks()[0], this.userMediaStream);
        stream.getTracks().forEach(t => this.myTracksForCleanup.push(t));
      });
    }
  }

  private async getCameraTrack(device: MediaDeviceInfo): Promise<MediaStream> {
    let stream = await navigator.mediaDevices.getUserMedia({video: {deviceId: device.deviceId}, audio: false});
    return stream;
    if (!device.label.includes("QC Back Camera")) {
      //return stream;
    }
    let videoTrack = stream.getVideoTracks()[0];

    const mediaSwitcher = new MediaSwitcher();
    const outputStream = await mediaSwitcher.initialize(stream) as MediaStream;
    let canvas;
    let video;
    const onPauseSignal = () => {
      canvas = document.createElement("canvas");
      video = document.createElement("video");
      video.srcObject = stream;
      canvas.width = videoTrack.getSettings().width;
      canvas.height = videoTrack.getSettings().height;
      const ctx = canvas.getContext("2d");
      ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

      const mediaStream = canvas.captureStream(1);
      stream.getTracks().forEach(t => t.stop());
      mediaSwitcher.changeTrack(mediaStream.getVideoTracks()[0]);
      stream = mediaStream;
      videoTrack = stream.getVideoTracks()[0];
    };

    const onResumeSignal = async () => {
      const mediaStream = await navigator.mediaDevices.getUserMedia({video: {deviceId: device.deviceId}, audio: false});
      stream.getTracks().forEach(t => t.stop());
      if(canvas){
        canvas.remove();
        canvas = null;
      }
      if(video){
        video.remove();
        video = null;
      }
      mediaSwitcher.changeTrack(mediaStream.getVideoTracks()[0]);
      stream = mediaStream;
      videoTrack = stream.getVideoTracks()[0];
    };

    let ws = new WebSocket("ws://127.0.0.1:8080");
    const onWsMessage = (event) => {
      if (event.data === "pause") {
        onPauseSignal();
      } else if (event.data === "resume") {
        onResumeSignal();
      }
    };
    const onWsError = (event) => {
      ws.close();
      ws = new WebSocket("ws://127.0.0.1:8080");
      ws.onmessage = onWsMessage;
      ws.onerror = onWsError;
    }
    ws.onmessage = onWsMessage;
    ws.onerror = onWsError;



    const onStopStream = () => {
      stream.getTracks().forEach(t => t.stop());
      if(canvas){
        canvas.remove();
        canvas = null;
      }
      if(video){
        video.remove();
        video = null;
      }
      ws.close();
    };

    //create new stream from canvas, and swap the stop function
    outputStream.getTracks().forEach(t => t.stop = onStopStream);
    return outputStream;
  }

  leaveCall() {
    this.videoClient.leaveVideoSession(new LeaveVideoSessionCommand({videoMemberId: this.myMember.id})).subscribe();
    this.myTracksForCleanup.forEach(t => t.stop());
    this.webRtcConnectionDetails.forEach(c => c.pc.close());

    this.onLeaveCall.emit();
  }

  async showCameraPopper() {
    console.log("show camera popper");
    if(this.availableMediaDevicesSubject.value.length === 0){
      await this.getMediaDevices();
    }
    this.cameraButtonElement.placement = NgxPopperjsPlacements.TOP;
    this.cameraButtonElement.closeOnClickOutside = true;
    this.cameraButtonElement.scheduledShow(0);
  }


  async showMicrophonePopper() {
    console.log("show microphone popper");
    if(this.availableMediaDevicesSubject.value.length === 0){
      await this.getMediaDevices();
    }
    this.microphoneButtonElement.placement = NgxPopperjsPlacements.TOP;
    this.microphoneButtonElement.closeOnClickOutside = true;
    this.microphoneButtonElement.scheduledShow(0);
  }
}


class MediaSwitcher {

  inputPeerConnection: RTCPeerConnection;
  outputPeerConnection: RTCPeerConnection;

  //  Change the entire input stream
  changeStream = function (stream) {
    if (
      !stream ||
      stream.constructor.name !== 'MediaStream' ||
      !this.inputPeerConnection ||
      !this.outputPeerConnection ||
      this.inputPeerConnection.connectionState !== 'connected' ||
      this.outputPeerConnection.connectionState !== 'connected'
    ) return;

    stream.getTracks.forEach(track => {
      this.changeTrack(track);
    })
  }

  //  Change one input track
  changeTrack = function (track) {
    if (
      !track ||
      (track.constructor.name !== 'MediaStreamTrack' && track.constructor.name !== 'CanvasCaptureMediaStreamTrack') ||
      !this.inputPeerConnection ||
      !this.outputPeerConnection ||
      this.inputPeerConnection.connectionState !== 'connected' ||
      this.outputPeerConnection.connectionState !== 'connected'
    ) return;

    const senders = this.inputPeerConnection.getSenders().filter(sender => !!sender.track && sender.track.kind === track.kind);
    if (!!senders.length)
      senders[0].replaceTrack(track);
  }

  //  Call this to, you guessed, initialize the class
  initialize = function (inputStream) {

    return new Promise(async (resolve, reject) => {

      //  ---------------------------------------------------------------------------------------
      //  Create input RTC peer connection
      //  ---------------------------------------------------------------------------------------
      this.inputPeerConnection = new RTCPeerConnection(null);
      this.inputPeerConnection.onicecandidate = e =>
        this.outputPeerConnection.addIceCandidate(e.candidate)
          .catch(err => reject(err));
      this.inputPeerConnection.ontrack = e => console.log(e.streams[0]);

      //  ---------------------------------------------------------------------------------------
      //  Create output RTC peer connection
      //  ---------------------------------------------------------------------------------------
      this.outputPeerConnection = new RTCPeerConnection(null);
      this.outputPeerConnection.onicecandidate = e =>
        this.inputPeerConnection.addIceCandidate(e.candidate)
          .catch(err => reject(err));
      this.outputPeerConnection.ontrack = e => {

        //  Set bitrate between the peers
        const sender = this.inputPeerConnection.getSenders()[0];
        const parameters = sender.getParameters();
        if (!parameters.encodings)
          parameters.encodings = [{}];

        //  Bitrate is 50 to 100 Mbit
        parameters.encodings[0].minBitrate = 30000000;
        parameters.encodings[0].maxBitrate = 100000000;

        sender.setParameters(parameters)
          .then(() => resolve(e.streams[0]))
          .catch(e => reject(e));
      }

      //  ---------------------------------------------------------------------------------------
      //  Get video source
      //  ---------------------------------------------------------------------------------------

      //  Create input stream
      if (!inputStream || inputStream.constructor.name !== 'MediaStream') {
        reject(new Error('Input stream is nonexistent or invalid.'));
        return;
      }

      //  Add stream to input peer
      inputStream.getTracks().forEach(track => {
        if (track.kind === 'video')
          this.videoSender = this.inputPeerConnection.addTrack(track, inputStream);
        if (track.kind === 'audio')
          this.audioSender = this.inputPeerConnection.addTrack(track, inputStream);
      });

      //  ---------------------------------------------------------------------------------------
      //  Make RTC call
      //  ---------------------------------------------------------------------------------------

      const offer = await this.inputPeerConnection.createOffer();
      await this.inputPeerConnection.setLocalDescription(offer);
      await this.outputPeerConnection.setRemoteDescription(offer);

      const answer = await this.outputPeerConnection.createAnswer();
      await this.outputPeerConnection.setLocalDescription(answer);
      await this.inputPeerConnection.setRemoteDescription(answer);
    });
  }
}

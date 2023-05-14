import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ComponentRef,
  Input,
  OnInit,
  ViewChild,
  ViewContainerRef,
  ViewEncapsulation,
  ViewRef
} from '@angular/core';
import {VideoClient, VideoMemberDto, VideoSessionDto} from "../../web-api-client";
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


  constructor(private videoClient: VideoClient,
              private videoFacade: VideoFacade,
              private notificationService: NotificationService,
              private signalRService: SignalRService,
              private cdr: ChangeDetectorRef) {

  }

  ngOnInit(): void {
  }

  async ngAfterViewInit() {
    await this.getMediaDevices();
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
    this.createUserMediaStream();
    this.joinSignalingSession();
    console.log("announcing myself");
    this.signalingChannel.subject.next({joinId: this.myMember.id});
    return session
  }

  onSessionDetailChanged(session: VideoSessionDto) {

  }

  async startWebRtcConnection(memberId: string, amIPolite: boolean) {
    console.log("starting webrtc connection to", memberId, amIPolite ? " politely" : " impolitely");
    const pc = new RTCPeerConnection(environment.iceConfiguration);
    this.webRtcConnectionDetails[memberId] = {
      pc: pc,
      amIPolite: amIPolite,
      trackMap: new Map<MediaStreamTrack, RTCRtpSender>(),
      makingOffer: false,
      ignoreOffer: false,
    };
    try {
      this.userMediaStream.getTracks().forEach(track => pc.addTrack(track, this.userMediaStream));
      this.screenShareStream?.getTracks().forEach(track => pc.addTrack(track, this.screenShareStream));
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
    let connectionDetails = this.webRtcConnectionDetails[memberId];
    // if we do not have a connection to this member, create one
    if (!connectionDetails) {
      await this.startWebRtcConnection(memberId, true);
      connectionDetails = this.webRtcConnectionDetails[memberId];
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
        this.focusedStream = null;
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
    if(stream.id===this.focusedStream){
      Array.from(this.mediaStreamComponentMap.values()).forEach(c=>{
        //if the element is not inside normalListView, move it there
        if(!this.normalListView.element.nativeElement.contains(c.location.nativeElement)){
          this.normalListView.insert(c.hostView);
        }
      });
      this.focusedStream=null;
    }
    else{
      //move all elements that are not inside focusedListView to focusedListView
      Array.from(this.mediaStreamComponentMap.values()).forEach(c=>{
        if(!this.focusedListView.element.nativeElement.contains(c.location.nativeElement)){
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
      this.focusedStream=stream.id;
    }
    this.cdr.detectChanges();
  }
/*
  focusStream(stream?: MediaStream) {
    if (stream) {
      const componentRef = this.mediaStreamComponentMap.get(stream.id);
      if (!componentRef) {
        console.warn("stream not found in map", stream);
        return;
      }
      if (stream.id === this.focusedStream) {
        //focused stream clicked, unfocus
        this.focusStream();
        return;
      }
      if (this.focusedStream) {
        //move the current focused stream to focusedListView and move this stream to focusedView
        const focusedRef = this.focusedView.detach();
        if (!focusedRef) {
          console.warn("focused stream not found in focusedView", this.focusedStream);
          return;
        }
        //move to focusedListView

        this.focusedListView.insert(focusedRef);

        const newFocusedRef = this.mediaStreamComponentMap.get(stream.id);
        if (!newFocusedRef) {
          console.warn("new focused stream not found in map", stream.id);
          return;
        }
        //move to focusedView
        const index=this.focusedListView.indexOf(newFocusedRef.hostView);
        const detachedRef=this.focusedListView.detach(index);
        this.focusedView.insert(detachedRef);
        this.focusedStream = stream.id;
      } else {
        const newFocusedRef = this.mediaStreamComponentMap.get(stream.id);
        if (!newFocusedRef) {
          console.warn("new focused stream not found in map", stream.id);
          return;
        }
        //move to focusedView
        const index=this.normalListView.indexOf(newFocusedRef.hostView);
        const detachedRef=this.focusedListView.detach(index);
        this.focusedView.insert(detachedRef);

        //move all other streams on normalListView to focusedListView
        const viewRefs: ViewRef[] = [];
        while (true) {
          const ref = this.normalListView.detach();
          if (!ref) {
            break;
          }
          viewRefs.push(ref);
        }
        viewRefs.forEach(ref => this.focusedListView.insert(ref));//todo does this revert the order?

      }
    } else {
      if (!this.focusedStream) {
        console.warn("cannot unfocus, no stream focused")
        return;
      }
      //to unfocus, move all streams on focusedListView and focusedView to normalListView
      const viewRefs: ViewRef[] = [];
      viewRefs.push(this.focusedView.detach());
      while (true) {
        const ref = this.focusedListView.detach();
        if (!ref) {
          break;
        }
        viewRefs.push(ref);
      }
      viewRefs.forEach(ref => this.normalListView.insert(ref));//todo does this revert the order?
      this.focusedStream = null;
    }
    this.cdr.detectChanges();
  }
*/

  addLocalTrack(track: MediaStreamTrack, stream: MediaStream) {
    console.log("adding local track", track, stream);
    stream.addTrack(track);
    for (let connectionDetails of Object.values(this.webRtcConnectionDetails)) {
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
    for (let connectionDetails of Object.values(this.webRtcConnectionDetails)) {
      const sender = connectionDetails.trackMap.get(track);
      if (sender) {
        connectionDetails.pc.removeTrack(sender);
      }
    }
  }

  createUserMediaStream() {
    //create dummy stream with a silent audio track to pass to the media switcher
    this.userMediaStream = new MediaStream();
    const audioContext = new AudioContext();
    const destination = audioContext.createMediaStreamDestination();
    this.addLocalTrack(destination.stream.getAudioTracks()[0], this.userMediaStream);
    //bind the stream to my video preview
    this.addStreamToUi(this.userMediaStream, this.myMember.id);
  }

  public async getMediaDevices() {
    //ask for permission to use microphone and camera
    try {
      await navigator.mediaDevices.getUserMedia({video: true, audio: true})
    } catch (e) {
      this.notificationService.error(`You need to allow access to your microphone and camera to use this feature. Please refresh the page and try again.`);
    }
    const devices = await navigator.mediaDevices.enumerateDevices();
    this.availableMediaDevicesSubject.next(devices);
    const defaultCamera = devices.find(d => d.deviceId === localStorage.getItem("defaultCamera"));
    const defaultMicrophone = devices.find(d => d.deviceId === localStorage.getItem("defaultMicrophone"));
    if (defaultCamera) {
      this.selectedCamera = defaultCamera;
    }
    if (defaultMicrophone) {
      this.selectedMicrophone = defaultMicrophone;
    }
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
        this.microphoneButtonElement.placement = NgxPopperjsPlacements.TOP;
        this.microphoneButtonElement.closeOnClickOutside = true;
        this.microphoneButtonElement.scheduledShow(0);
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
        this.cameraButtonElement.placement = NgxPopperjsPlacements.TOP;
        this.cameraButtonElement.closeOnClickOutside = true;
        this.cameraButtonElement.scheduledShow(0);
      }
    }
  }

  toggleScreenShare() {
    if (this.screenShareStream) {
      this.stopScreenShare();
    } else {
      navigator.mediaDevices.getDisplayMedia({video: true,audio:true}).then(stream => {
        this.screenShareStream = stream;
        stream.getTracks().forEach(t => this.addLocalTrack(t, this.screenShareStream));
        //listen to outside stream stop
        stream.getVideoTracks()[0].addEventListener('ended', () => {
          this.stopScreenShare();
        });
        this.addStreamToUi(stream, this.myMember.id);
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
      navigator.mediaDevices.getUserMedia({video: {deviceId: device.deviceId},audio:false}).then(stream => {
        if (stream.getVideoTracks().length !== 1) {
          console.warn("unexpected number of video tracks when setting camera", stream.getVideoTracks());
        }
        this.addLocalTrack(stream.getVideoTracks()[0], this.userMediaStream);
      });
    }
  }

}

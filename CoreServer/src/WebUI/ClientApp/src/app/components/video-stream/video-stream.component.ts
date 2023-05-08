import {AfterViewInit, Component, ElementRef, Input, OnInit, ViewChild} from '@angular/core';
import {VideoStreamDto} from "../../web-api-client";

@Component({
  selector: 'app-video-stream',
  templateUrl: './video-stream.component.html',
  styleUrls: ['./video-stream.component.css']
})
export class VideoStreamComponent implements OnInit,AfterViewInit {
  @Input() stream: VideoStreamDto;
  @ViewChild('video') video:ElementRef;

  private webSocket: WebSocket;
  private peerConnection: RTCPeerConnection;
  private restartTimeout = null;
  private terminated=false;

  constructor() {
  }

  ngOnInit(): void {
  }

  ngAfterViewInit() {
    this.start();
  }

  start() {
    this.webSocket=new WebSocket(`ws://localhost:8889/${this.stream.id}/ws`);
    this.webSocket.onerror = (event) => {
      this.scheduleRestart();
      event.preventDefault();
    }
    this.webSocket.onmessage = (event) => {
      this.onIceServer(event);
    }

  }
  stop(){
    this.terminated=true;
    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }
    if (this.webSocket) {
      this.webSocket.close();
      this.webSocket = null;
    }
  }

  private onIceServer(msg: MessageEvent) {
    if (this.webSocket === null) {
      return;
    }

    const iceServers = JSON.parse(msg.data);

    this.peerConnection = new RTCPeerConnection({
      iceServers,
    });
    this.webSocket.onmessage = (msg) => this.onRemoteDescription(msg);
    this.peerConnection.onicecandidate = (evt) => this.onIceCandidate(evt);
    this.peerConnection.oniceconnectionstatechange = () => {
      if (this.peerConnection === null) {
        return;
      }

      console.log("peer connection state:", this.peerConnection.iceConnectionState);

      switch (this.peerConnection.iceConnectionState) {
        case "disconnected":
          this.scheduleRestart();
      }
    };
    this.peerConnection.ontrack = (evt) => {
      console.log("new track " + evt.track.kind);
      this.video.nativeElement.srcObject = evt.streams[0];
    };
    const direction = "sendrecv";
    this.peerConnection.addTransceiver("video", { direction });
    this.peerConnection.addTransceiver("audio", { direction });

    this.peerConnection.createOffer()
      .then((desc) => {
        if (this.peerConnection === null || this.webSocket === null) {
          return;
        }

        this.peerConnection.setLocalDescription(desc);

        console.log("sending offer");
        this.webSocket.send(JSON.stringify(desc));
      });
  }
  onRemoteDescription(msg) {
    if (this.peerConnection === null || this.webSocket === null) {
      return;
    }

    this.peerConnection.setRemoteDescription(new RTCSessionDescription(JSON.parse(msg.data)));
    this.webSocket.onmessage = (msg) => this.onRemoteCandidate(msg);
  }
  onIceCandidate(evt) {
    if (this.webSocket === null) {
      return;
    }

    if (evt.candidate !== null) {
      if (evt.candidate.candidate !== "") {
        this.webSocket.send(JSON.stringify(evt.candidate));
      }
    }
  }

  onRemoteCandidate(msg) {
    if (this.peerConnection === null) {
      return;
    }

    this.peerConnection.addIceCandidate(JSON.parse(msg.data));
  }
  scheduleRestart() {
    this.stop();

    this.restartTimeout = window.setTimeout(() => {
      this.restartTimeout = null;
      this.start();
    }, 2000);
  }
}

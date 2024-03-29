import {Injectable} from '@angular/core';
import {
  VideoMemberDto,
  VideoSessionDto,
  VideoStreamDto
} from "../../web-api-client";
import {RpcService} from "./rpc.service";
import {SignalRService} from "../signalr.service";
import {from, Observable, ReplaySubject} from "rxjs";
import {map, mergeMap} from "rxjs/operators";
import {IRpcVideoService} from "../../models/interfaces/RpcVideoService";
import {VideoFacade} from "../video-facade.service";

@Injectable({
  providedIn: 'root'
})
export class RpcVideoService extends RpcService implements IRpcVideoService {

  constructor(private signalRService: SignalRService, private videoFacade: VideoFacade) {
    super(signalRService, "RpcVideoService", {
      UpdateVideoSession: (videoSession: VideoSessionDto) => this.UpdateVideoSession(videoSession),
      UpdateVideoMember: (videoSessionMember: VideoMemberDto) => this.UpdateVideoMember(videoSessionMember),
      UpdateVideoStream: (videoStream: VideoStreamDto) => this.UpdateVideoStream(videoStream),
    });
  }

  SendVideoStream(observable: Observable<Blob>, id: string, accessKey: string) {
    const blobToUint8Array = async (blob: Blob) => {
      const arrayBuffer = await new Response(blob).arrayBuffer();
      return new Uint8Array(arrayBuffer);
    }
    const uint8Stream = observable.pipe(mergeMap(value => from(blobToUint8Array(value))));
    return this.signalRService.stream("UploadVideoStream", uint8Stream, id, accessKey);
    //return this.signalRService.dataStream(id,observable);
  }

  public getVideoStream(id: string): Observable<Uint8Array> {
    return this.signalRService.getStream<Uint8Array>(id);
  }

  UpdateVideoSession(videoSession: VideoSessionDto) {
    this.videoFacade.addOrReplaceSession(videoSession)
    console.log("UpdateVideoSession", videoSession);
  }

  UpdateVideoMember(videoSessionMember: VideoMemberDto) {
    this.videoFacade.updateVideoMember(videoSessionMember);
    console.log("UpdateVideoSessionMember", videoSessionMember);
  }

  UpdateVideoStream(videoStream: VideoStreamDto) {
    console.log("UpdateVideoStream", videoStream);
    this.videoFacade.updateVideoStream(videoStream);
  }


}

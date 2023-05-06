import {VideoMemberDto, VideoSessionDto, VideoStreamDto} from "../../web-api-client";

export interface IRpcVideoService {
  UpdateVideoSession(videoSession: VideoSessionDto);

  UpdateVideoStream(videoStream: VideoStreamDto);

  UpdateVideoMember(videoSessionMember: VideoMemberDto);
}

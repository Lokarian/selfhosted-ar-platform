using CoreServer.Application.Video.Queries.GetMyVideoSessions;
using CoreServer.Application.RPC.common;
using CoreServer.Application.Video.Queries;
using CoreServer.Application.Video.Queries.Dtos;

namespace CoreServer.Application.RPC;

public interface IRpcVideoService : IRpcService
{
    Task UpdateVideoSession(VideoSessionDto videoSession);
    Task UpdateVideoStream(VideoStreamDto videoStream);
    Task UpdateVideoMember(VideoMemberDto videoSessionMember);

}
using AutoMapper;
using CoreServer.Application.Video.Commands.CreateVideoSession;
using CoreServer.Application.Video.Commands.CreateVideoStream;
using CoreServer.Application.Video.Commands.JoinVideoSession;
using CoreServer.Application.Video.Commands.LeaveVideoSession;
using CoreServer.Application.Video.Commands.StopVideoStream;
using CoreServer.Application.Video.Queries;
using CoreServer.Application.Video.Queries.Dtos;
using CoreServer.Application.Video.Queries.GetMyVideoSessions;
using CoreServer.Application.Video.Queries.GetVideoSessionMembers;
using CoreServer.Application.Video.Queries.GetVideoStreams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

[Authorize]
public class VideoController : ApiControllerBase
{
    private readonly IMapper _mapper;

    public VideoController(IMapper mapper)
    {
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<VideoSessionDto>> GetMyVideoSessions()
    {
        return Ok(await Mediator.Send(new GetMyVideoSessionsQuery()));
    }

    [HttpGet]
    public async Task<ActionResult<List<VideoMemberDto>>> GetVideoSessionMembers([FromQuery] Guid sessionId)
    {
        return Ok(await Mediator.Send(new GetVideoSessionMembersQuery() { VideoSessionId = sessionId }));
    }

    [HttpGet]
    public async Task<ActionResult<List<VideoStreamDto>>> GetVideoStreams([FromQuery] Guid sessionId)
    {
        return Ok(await Mediator.Send(new GetVideoStreamsQuery() { VideoSessionId = sessionId }));
    }

    [HttpPost]
    public async Task<ActionResult<VideoSessionDto>> CreateVideoSession(CreateVideoSessionCommand command)
    {
        var session = await Mediator.Send(command);
        return Ok(_mapper.Map<VideoSessionDto>(session));
    }

    [HttpPost]
    public async Task<ActionResult<VideoStreamDto>> RequestVideoStream(CreateVideoStreamCommand command)
    {
        var stream = await Mediator.Send(command);
        return Ok(_mapper.Map<VideoStreamDto>(stream));
    }

    [HttpPost]
    public async Task<ActionResult<VideoStreamDto>> StopVideoStream(StopVideoStreamCommand command)
    {
        var stream = await Mediator.Send(command);
        return Ok(_mapper.Map<VideoStreamDto>(stream));
    }

    [HttpPost]
    public async Task<ActionResult<Tuple<VideoMemberDto, string>>> JoinVideoSession(JoinVideoSessionCommand command)
    {
        var member = await Mediator.Send(command);
        var accessKey = member.AccessKey;
        return Ok(new Tuple<VideoMemberDto, string>(_mapper.Map<VideoMemberDto>(member), accessKey));
    }

    [HttpPost]
    public async Task<ActionResult> LeaveVideoSession(LeaveVideoSessionCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

}
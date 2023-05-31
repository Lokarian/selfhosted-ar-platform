using AutoMapper;
using CoreServer.Application.AR.Commands.CreateArSession;
using CoreServer.Application.AR.Commands.JoinArSession;
using CoreServer.Application.AR.Commands.LeaveArSession;
using CoreServer.Application.AR.Queries.GetArSessionMembers;
using CoreServer.Application.AR.Queries.GetMyArSessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

[Authorize]
public class ArController : ApiControllerBase
{
    private readonly IMapper _mapper;

    public ArController(IMapper mapper)
    {
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<List<ArSessionDto>>> GetMyArSessions()
    {
        return Ok(await Mediator.Send(new GetMyArSessionsQuery()));
    }

    [HttpGet]
    public async Task<ActionResult<List<ArMemberDto>>> GetArSessionMembers([FromQuery] Guid sessionId)
    {
        return Ok(await Mediator.Send(new GetArSessionMembersQuery() { ArSessionId = sessionId }));
    }

    [HttpPost]
    public async Task<ActionResult<ArSessionDto>> CreateArSession(CreateArSessionCommand command)
    {
        var session = await Mediator.Send(command);
        return Ok(_mapper.Map<ArSessionDto>(session));
    }

    [HttpPost]
    public async Task<ActionResult<ArMemberDto>> JoinArSession(JoinArSessionCommand command)
    {
        var member = await Mediator.Send(command);
        return Ok(_mapper.Map<ArMemberDto>(member));
    }

    [HttpPost]
    public async Task<ActionResult> LeaveArSession(LeaveArSessionCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

}
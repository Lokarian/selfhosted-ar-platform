using AutoMapper;
using CoreServer.Application.Chat.Commands;
using CoreServer.Application.Chat.Commands.DeleteChatMessage;
using CoreServer.Application.Chat.Queries.GetChatMessages;
using CoreServer.Application.Chat.Queries.GetMyChatSessions;
using CoreServer.Application.Session.Commands.CreateSession;
using CoreServer.Application.Session.Commands.UpdateSession;
using CoreServer.Application.Session.Queries.GetMySessions;
using CoreServer.Domain.Entities.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

[Authorize]
public class SessionController : ApiControllerBase
{
    private readonly IMapper _mapper;

    public SessionController(IMapper mapper)
    {
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionDto>>> GetMySessions()
    {
        return Ok(await Mediator.Send(new GetMySessionsQuery()));
    }
    

    [HttpPost]
    public async Task<ActionResult<SessionDto>> CreateSession(CreateSessionCommand command)
    {
        var session = await Mediator.Send(command);
        return Ok(_mapper.Map<SessionDto>(session));
    }

    [HttpPost]
    public async Task<ActionResult<SessionDto>> UpdateSession(UpdateSessionCommand command)
    {
        var session = await Mediator.Send(command);
        return Ok(_mapper.Map<SessionDto>(session));
    }
}
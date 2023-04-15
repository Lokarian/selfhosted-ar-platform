using AutoMapper;
using CoreServer.Application.Chat.Commands;
using CoreServer.Application.Chat.Commands.CreateChatSession;
using CoreServer.Application.Chat.Queries;
using CoreServer.Domain.Entities.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreServer.WebUI.Controllers;

[Authorize]
public class ChatController : ApiControllerBase
{
    private readonly IMapper _mapper;

    public ChatController(IMapper mapper)
    {
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChatSessionDto>>> GetMyChatSessions()
    {
        return Ok(await Mediator.Send(new GetMyChatSessionsQuery()));
    }

    [HttpGet]
    public async Task<ActionResult<ChatMessageDto>> GetChatMessages(GetChatMessagesQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    [HttpPost]
    public async Task<ActionResult<ChatSession>> CreateChatSession(CreateChatSessionCommand command)
    {
        var chatSession = await Mediator.Send(command);
        return Ok(_mapper.Map<ChatSessionDto>(chatSession));
    }

    [HttpPost("message")]
    public async Task<ActionResult<ChatMessageDto>> SendMessageToChatSession(SendMessageToChatSessionCommand command)
    {
        ChatMessage chatMessage = await Mediator.Send(command);
        return Ok(_mapper.Map<ChatMessageDto>(chatMessage));
    }

    [HttpDelete("message/{id}")]
    public async Task<ActionResult> DeleteChatMessage(Guid id)
    {
        await Mediator.Send(new DeleteChatMessageCommand { Id = id });
        return NoContent();
    }
}
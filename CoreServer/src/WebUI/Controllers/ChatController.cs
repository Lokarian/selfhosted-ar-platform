using AutoMapper;
using CoreServer.Application.Chat.Commands;
using CoreServer.Application.Chat.Commands.CreateChatSession;
using CoreServer.Application.Chat.Commands.DeleteChatMessage;
using CoreServer.Application.Chat.Commands.SendMessageToChatSession;
using CoreServer.Application.Chat.Commands.UpdateChatSession;
using CoreServer.Application.Chat.Queries.GetChatMembers;
using CoreServer.Application.Chat.Queries.GetChatMessages;
using CoreServer.Application.Chat.Queries.GetMyChatSessions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Application.User.Queries;
using CoreServer.Domain.Entities.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

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
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetChatMessages([FromQuery] GetChatMessagesQuery query)
    {
        return Ok(await Mediator.Send(query));
    }

    [HttpPost]
    public async Task<ActionResult<ChatSessionDto>> CreateChatSession(CreateChatSessionCommand command)
    {
        ChatSession chatSession = await Mediator.Send(command);
        return Ok(_mapper.Map<ChatSessionDto>(chatSession));
    }

    [HttpPost]
    public async Task<ActionResult<ChatMessageDto>> SendMessageToChatSession(SendMessageToChatSessionCommand command)
    {
        ChatMessage chatMessage = await Mediator.Send(command);
        return Ok(_mapper.Map<ChatMessageDto>(chatMessage));
    }
    [HttpPost]
    public async Task<ActionResult<ChatSessionDto>> UpdateChatSession(UpdateChatSessionCommand command)
    {
        ChatSession chatSession = await Mediator.Send(command);
        return Ok(_mapper.Map<ChatSessionDto>(chatSession));
    }
    [HttpPost]
    public async Task<ActionResult> UpdateLastRead(UpdateChatSessionLastReadCommand command)
    {
        return Ok(await Mediator.Send(command));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteChatMessage(Guid id)
    {
        await Mediator.Send(new DeleteChatMessageCommand { Id = id });
        return NoContent();
    }
}
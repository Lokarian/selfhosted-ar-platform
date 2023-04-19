using AutoMapper;
using CoreServer.Application.Chat.Commands;
using CoreServer.Application.Chat.Commands.CreateChatSession;
using CoreServer.Application.Chat.Queries;
using CoreServer.Application.Chat.Queries.GetSessionMembers;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPCInterfaces;
using CoreServer.Application.User.Queries;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Infrastructure.RPC;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CoreServer.WebUI.Controllers;

[Authorize]
public class ChatController : ApiControllerBase
{
    private readonly IMapper _mapper;
    private readonly IUserProxy<IRpcChatClient> _chatProxy;
    private readonly IUserProxy<IRpcUserClient> _userProxy;
    private readonly ICurrentUserService _currentUserService;

    public ChatController(IMapper mapper, IUserProxy<IRpcChatClient> chatProxy,ICurrentUserService currentUserService, IUserProxy<IRpcUserClient> userProxy)
    {
        _mapper = mapper;
        _chatProxy = chatProxy;
        _currentUserService = currentUserService;
        _userProxy = userProxy;
    }
    [HttpGet]
    public async Task<ActionResult> TestSignalR()
    {
        await _chatProxy.Client(_currentUserService.User!.Id).UpdateChatSession(new ChatSessionDto(){Id = new Guid(),Members = new List<ChatMemberDto>(),Name = "test",LastMessage = null});
        return Ok();
    }
    [HttpGet]
    public async Task<ActionResult> TestSignalR2()
    {
        await _userProxy.Client(_currentUserService.User!.Id).UpdateUser(new AppUserDto(){Id = Guid.NewGuid(),UserName = "signalRUser"});
        return Ok();
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
    public async Task<ActionResult<ChatSessionDto>> CreateChatSession(CreateChatSessionCommand command)
    {
        var chatSession = await Mediator.Send(command);
        return Ok(_mapper.Map<ChatSessionDto>(chatSession));
    }

    [HttpPost]
    public async Task<ActionResult<ChatMessageDto>> SendMessageToChatSession(SendMessageToChatSessionCommand command)
    {
        ChatMessage chatMessage = await Mediator.Send(command);
        return Ok(_mapper.Map<ChatMessageDto>(chatMessage));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteChatMessage(Guid id)
    {
        await Mediator.Send(new DeleteChatMessageCommand { Id = id });
        return NoContent();
    }
}
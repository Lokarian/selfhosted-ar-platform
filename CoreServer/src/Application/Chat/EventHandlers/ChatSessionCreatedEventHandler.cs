using AutoMapper;
using CoreServer.Application.Chat.Queries;
using CoreServer.Application.Chat.Queries.GetMyChatSessions;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Domain.Events;
using CoreServer.Domain.Events.Chat;
using MediatR;

namespace CoreServer.Application.Chat.EventHandlers;

public class ChatSessionCreatedEventHandler : INotificationHandler<ChatSessionCreatedEvent>
{
    private readonly IMapper _mapper;
    private readonly IUserProxy<IRpcChatService> _rpcService;

    public ChatSessionCreatedEventHandler(IUserProxy<IRpcChatService> rpcService, IMapper mapper)
    {
        _rpcService = rpcService;
        _mapper = mapper;
    }

    public async Task Handle(ChatSessionCreatedEvent notification, CancellationToken cancellationToken)
    {
        await (await _rpcService.Clients(notification.Session.Members.Select(m => m.UserId))).UpdateChatSession(
            _mapper.Map<ChatSessionDto>(notification.Session));
    }
}
using AutoMapper;
using CoreServer.Application.Chat.Queries.GetMyChatSessions;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Domain.Events.Chat;
using MediatR;

namespace CoreServer.Application.Chat.EventHandlers;

public class ChatSessionUpdatedEventHandler :  INotificationHandler<ChatSessionUpdatedEvent>
{
    private readonly IMapper _mapper;
    private readonly IUserProxy<IRpcChatService> _rpcService;

    public ChatSessionUpdatedEventHandler(IUserProxy<IRpcChatService> rpcService, IMapper mapper)
    {
        _rpcService = rpcService;
        _mapper = mapper;
    }

    public async Task Handle(ChatSessionUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var userIds= notification.RemovedUsers.Select(u => u.Id).Concat(notification.Session.Members.Select(m => m.UserId));
        var dto = _mapper.Map<ChatSessionDto>(notification.Session);
        await (await _rpcService.Clients(userIds)).UpdateChatSession(dto);
    }
}
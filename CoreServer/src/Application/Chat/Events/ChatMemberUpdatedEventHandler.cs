using AutoMapper;
using CoreServer.Application.Chat.Queries.GetChatMembers;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Domain.Events.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.EventHandlers;

public class ChatMemberUpdatedEventHandler : INotificationHandler<ChatMemberUpdatedEvent>
{
    private readonly IUserProxy<IRpcChatService> _userProxy;
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public ChatMemberUpdatedEventHandler(IUserProxy<IRpcChatService> userProxy, IApplicationDbContext context, IMapper mapper)
    {
        _userProxy = userProxy;
        _context = context;
        _mapper = mapper;
    }

    public async Task Handle(ChatMemberUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var receivers = _context.ChatMembers.Where(x => x.SessionId == notification.ChatMember.SessionId)
            .Select(x => x.BaseMember.UserId);
        var proxy = await _userProxy.Clients(receivers);
        await proxy.UpdateChatMember(_mapper.Map<ChatMemberDto>(notification.ChatMember));
    }
}
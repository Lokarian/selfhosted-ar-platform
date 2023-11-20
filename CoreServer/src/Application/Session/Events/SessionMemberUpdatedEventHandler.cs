using AutoMapper;
using CoreServer.Application.Chat.Queries.GetChatMembers;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Application.Session.Queries.GetMySessions;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Events.Chat;
using CoreServer.Domain.Events.Session;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.EventHandlers;

public class SessionMemberUpdatedEventHandler : INotificationHandler<SessionMemberUpdatedEvent>
{
    private readonly IUserProxy<IRpcSessionService> _userProxy;
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public SessionMemberUpdatedEventHandler(IUserProxy<IRpcSessionService> userProxy, IApplicationDbContext context,
        IMapper mapper)
    {
        _userProxy = userProxy;
        _context = context;
        _mapper = mapper;
    }

    public async Task Handle(SessionMemberUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var receivers = _context.SessionMembers
            .Where(x => x.SessionId == notification.SessionMember.SessionId && x.DeletedAt != null)
            .Select(x => x.UserId);
        var proxy = await _userProxy.Clients(receivers);
        await proxy.UpdateSessionMember(_mapper.Map<SessionMemberDto>(notification.SessionMember));
    }
}
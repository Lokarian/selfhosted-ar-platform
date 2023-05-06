using AutoMapper;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Application.Session.Queries.GetMySessions;
using CoreServer.Domain.Events.Session;
using MediatR;

namespace CoreServer.Application.Session.Events;

public class SessionCreatedEventHandler : INotificationHandler<SessionCreatedEvent>
{
    private readonly IMapper _mapper;
    private readonly IUserProxy<IRpcSessionService> _rpcService;

    public SessionCreatedEventHandler(IUserProxy<IRpcSessionService> rpcService, IMapper mapper)
    {
        _rpcService = rpcService;
        _mapper = mapper;
    }

    public async Task Handle(SessionCreatedEvent notification, CancellationToken cancellationToken)
    {
        await (await _rpcService.Clients(notification.Session.Members.Select(m => m.UserId))).UpdateSession(
            _mapper.Map<SessionDto>(notification.Session));
    }
}
using AutoMapper;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Application.Session.Queries.GetMySessions;
using CoreServer.Domain.Events.Session;
using MediatR;

namespace CoreServer.Application.Session.Events;

public class SessionUpdatedEventHandler : INotificationHandler<SessionUpdatedEvent>
{
    private readonly IMapper _mapper;
    private readonly IUserProxy<IRpcSessionService> _rpcService;

    public SessionUpdatedEventHandler(IUserProxy<IRpcSessionService> rpcService, IMapper mapper)
    {
        _rpcService = rpcService;
        _mapper = mapper;
    }

    public async Task Handle(SessionUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var userIds = notification.Session.Members.Select(m => m.UserId);
        var proxy = await _rpcService.Clients(userIds);
        await proxy.UpdateSession(_mapper.Map<SessionDto>(notification.Session));
        if (notification.RemovedUsers != null)
        {
            userIds = notification.RemovedUsers.Select(u => u.Id);
            proxy = await _rpcService.Clients(userIds);
            await proxy.RemoveSession(notification.Session.Id);
        }
    }
}
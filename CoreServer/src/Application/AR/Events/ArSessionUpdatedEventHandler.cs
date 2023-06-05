using AutoMapper;
using CoreServer.Application.AR.Queries.GetMyArSessions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Domain.Events.Ar;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Events;

public class ArSessionUpdatedEventHandler : INotificationHandler<ArSessionCreatedEvent>
{
    private readonly IUserProxy<IRpcArService> _userProxy;
    private readonly IMapper _mapper;
    private readonly IApplicationDbContext _context;

    public ArSessionUpdatedEventHandler(IUserProxy<IRpcArService> userProxy, IMapper mapper,
        IApplicationDbContext context)
    {
        _userProxy = userProxy;
        _mapper = mapper;
        _context = context;
    }

    public async Task Handle(ArSessionCreatedEvent notification, CancellationToken cancellationToken)
    {
        var session = await _context.ArSessions
            .Include(x => x.BaseSession)
            .ThenInclude(x => x.Members)
            .FirstOrDefaultAsync(x => x.BaseSessionId == notification.ArSession.BaseSessionId, cancellationToken);
        if (session == null)
        {
            return;
        }

        var receivers = session.BaseSession.Members.Select(x => x.UserId);
        var proxy = await _userProxy.Clients(receivers);
        await proxy.UpdateArSession(_mapper.Map<ArSessionDto>(session));
    }
}
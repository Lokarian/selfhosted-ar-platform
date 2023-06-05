using AutoMapper;
using CoreServer.Application.AR.Queries.GetArSessionMembers;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Domain.Events.Ar;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Events;

public class ArMemberUpdatedEventHandler : INotificationHandler<ArMemberUpdatedEvent>
{
    private readonly IUserProxy<IRpcArService> _userProxy;
    private readonly IMapper _mapper;
    private readonly IApplicationDbContext _context;

    public ArMemberUpdatedEventHandler(IUserProxy<IRpcArService> userProxy, IApplicationDbContext context, IMapper mapper)
    {
        _userProxy = userProxy;
        _context = context;
        _mapper = mapper;
    }

    public async Task Handle(ArMemberUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var session = await _context.ArSessions
            .Include(v => v.BaseSession)
            .ThenInclude(x => x.Members)
            .FirstOrDefaultAsync(x => x.Members.Any(m => m.Id == notification.ArMember.Id), cancellationToken);
        var receivers = session!.BaseSession.Members.Select(x => x.UserId);
        var proxy = await _userProxy.Clients(receivers);
        await proxy.UpdateArMember(_mapper.Map<ArMemberDto>(notification.ArMember));
    }
}
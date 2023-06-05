using CoreServer.Application.AR.Commands.LeaveArSession;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Events.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Events;

public class RemoveArMembersOnDisconnectHandler : INotificationHandler<UserConnectionDisconnectedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public RemoveArMembersOnDisconnectHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }


    public async Task Handle(UserConnectionDisconnectedEvent notification, CancellationToken cancellationToken)
    {
        var openArMembers = _context.ArMembers.AsTracking()
            .Where(x => x.UserConnectionId == notification.UserConnection.Id && x.DeletedAt == null);
        foreach (var x in openArMembers)
        {
            await _mediator.Send(new LeaveArSessionCommand() { ArMemberId = x.Id }, cancellationToken);
        }
    }
}
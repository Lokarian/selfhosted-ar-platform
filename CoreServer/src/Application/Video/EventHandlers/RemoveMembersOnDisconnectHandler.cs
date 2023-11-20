using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Video.Commands.LeaveVideoSession;
using CoreServer.Domain.Events.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.EventHandlers;

public class RemoveMembersOnDisconnectHandler : INotificationHandler<UserConnectionDisconnectedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public RemoveMembersOnDisconnectHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }


    public async Task Handle(UserConnectionDisconnectedEvent notification, CancellationToken cancellationToken)
    {
        var openVideoMembers = await _context.VideoMembers.AsTracking()
            .Where(x => x.UserConnectionId == notification.UserConnection.Id && x.DeletedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var x in openVideoMembers)
        {
            await _mediator.Send(new LeaveVideoSessionCommand() { VideoMemberId = x.Id }, cancellationToken);
        }
    }
}
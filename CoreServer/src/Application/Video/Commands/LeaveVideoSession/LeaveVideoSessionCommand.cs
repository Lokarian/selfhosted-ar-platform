using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Video;
using CoreServer.Domain.Events.Chat;
using CoreServer.Domain.Events.Video;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Commands.LeaveVideoSession;

public class LeaveVideoSessionCommand : IRequest
{
    public Guid VideoMemberId { get; set; }
}

public class LeaveVideoSessionCommandHandler : IRequestHandler<LeaveVideoSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public LeaveVideoSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task Handle(LeaveVideoSessionCommand request, CancellationToken cancellationToken)
    {
        var member = await _context.VideoMembers.FirstOrDefaultAsync(x => x.Id == request.VideoMemberId, cancellationToken);
        if (member == null)
        {
            throw new NotFoundException(nameof(VideoMember), _currentUserService.User!.Id);
        }

        member.DeletedAt = _dateTime.Now;
        member.AccessKey = null;
        member.AddDomainEvent(new VideoMemberUpdatedEvent(member));
        await _context.SaveChangesAsync(cancellationToken);
        return;
    }
}
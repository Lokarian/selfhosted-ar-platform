using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.AR;
using CoreServer.Domain.Events.Ar;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Commands.LeaveArSession;

public class LeaveArSessionCommand : IRequest
{
    public Guid ArMemberId { get; set; }
}

public class LeaveArSessionCommandHandler : IRequestHandler<LeaveArSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public LeaveArSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<Unit> Handle(LeaveArSessionCommand request, CancellationToken cancellationToken)
    {
        ArMember? member =
            await _context.ArMembers.FirstOrDefaultAsync(x => x.Id == request.ArMemberId, cancellationToken);
        if (member == null)
        {
            throw new NotFoundException(nameof(ArMember), _currentUserService.User!.Id);
        }

        member.DeletedAt = _dateTime.Now;
        member.AccessKey = null;
        member.AddDomainEvent(new ArMemberUpdatedEvent(member));
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
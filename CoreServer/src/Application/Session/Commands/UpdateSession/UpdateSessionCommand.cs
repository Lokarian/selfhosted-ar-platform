using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Events.Session;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Session.Commands.UpdateSession;

public record UpdateSessionCommand : IRequest<BaseSession>
{
    public Guid SessionId { get; init; }
    public List<Guid>? UserIds { get; init; }
    public string? Name { get; init; }
}

public class UpdateSessionCommandHandler : IRequestHandler<UpdateSessionCommand, BaseSession>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BaseSession> Handle(UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.BaseSessions
            .AsTracking()
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException(nameof(session), request.SessionId);
        }

        var membersToRemove = new List<SessionMember>();
        if (request.UserIds != null)
        {
            var users = await _context.AppUsers.Where(x => request.UserIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
            //get all session members that are not in the request
            membersToRemove = session.Members.Where(x => !request.UserIds.Contains(x.UserId)).ToList();
            //get all users that are not in the session
            var usersToAdd = users.Where(x => session.Members.All(y => y.UserId != x.Id)).ToList();
            //remove all members that are not in the request
            membersToRemove.ForEach(x => session.Members.Remove(x));
            //add all users that are not in the session
            usersToAdd.ForEach(x =>
            {
                var sessionMember = new SessionMember { SessionId = session.Id, UserId = x.Id };
                sessionMember.AddDomainEvent(new SessionMemberUpdatedEvent(sessionMember));
                session.Members.Add(sessionMember);
                _context.SessionMembers.Add(sessionMember);
            });
        }

        if (request.Name != null)
        {
            session.Name = request.Name;
        }

        session.AddDomainEvent(new SessionUpdatedEvent(session, membersToRemove.Select(m => m.User).ToList()));
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }
}
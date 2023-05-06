using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Events.Session;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Session.Commands.UpdateSession;

public record UpdateSessionCommand : IRequest<UserSession>
{
    public Guid SessionId { get; init; }
    public List<Guid>? UserIds { get; init; }
    public string? Name { get; init; }
}

public class UpdateChatSessionCommandHandler : IRequestHandler<UpdateSessionCommand, UserSession>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateChatSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UserSession> Handle(UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.UserSessions
            .Include(x => x.Members)
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
            usersToAdd.ForEach(x => session.Members.Add(new SessionMember { SessionId = session.Id, UserId = x.Id }));
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
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Events.Session;
using MediatR;
using Microsoft.EntityFrameworkCore;

//import System.Linq;

namespace CoreServer.Application.Session.Commands.CreateSession;

public class CreateSessionCommand : IRequest<BaseSession>
{
    public List<Guid> UserIds { get; set; } = null!;
    public string? Name { get; set; }
}

public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, BaseSession>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BaseSession> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        List<AppUser> users = await _context.AppUsers.AsTracking().Where(u => request.UserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        //add current user to  session if not already in it
        if (users.All(u => u.Id != _currentUserService.User!.Id))
        {
            users.Add(_currentUserService.User!);
        }

        var session = new BaseSession() { Name = request.Name };
        List<SessionMember> members = users.Select(u => new SessionMember(u, session)).ToList();

        _context.BaseSessions.Add(session);
        _context.SessionMembers.AddRange(members);
        session.AddDomainEvent(new SessionCreatedEvent(session));
        await _context.SaveChangesAsync(cancellationToken);

        return session;
    }
}
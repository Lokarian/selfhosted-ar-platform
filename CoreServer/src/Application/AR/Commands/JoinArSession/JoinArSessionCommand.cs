using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.AR;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Events.Ar;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Commands.JoinArSession;

public class JoinArSessionCommand : IRequest<ArMember>
{
    public Guid ArSessionId { get; set; }
    public ArUserRole Role { get; set; }
}

public class JoinArSessionCommandHandler : IRequestHandler<JoinArSessionCommand, ArMember>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public JoinArSessionCommandHandler(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ArMember> Handle(JoinArSessionCommand request, CancellationToken cancellationToken)
    {
        ArSession? ArSession = await _context.ArSessions
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.BaseSessionId == request.ArSessionId, cancellationToken);

        if (ArSession == null)
        {
            throw new NotFoundException(nameof(ArSession), request.ArSessionId);
        }

        SessionMember? baseMember = await _context.SessionMembers
            .FirstOrDefaultAsync(x => x.UserId == _currentUserService.User!.Id, cancellationToken);
        if (baseMember == null)
        {
            throw new NotFoundException(nameof(SessionMember), _currentUserService.User!.Id);
        }

        if (_currentUserService.Connection == null)
        {
            throw new NotFoundException(nameof(UserConnection), _currentUserService.User!.Id);
        }

        ArMember member = new ArMember
        {
            BaseMember = baseMember,
            Session = ArSession,
            AccessKey = RandomString.Generate(10),
            UserConnectionId = _currentUserService.Connection.Id,
            Role = request.Role
        };
        ArSession.Members.Add(member);
        member.AddDomainEvent(new ArMemberUpdatedEvent(member));
        _context.ArMembers.Add(member);
        await _context.SaveChangesAsync(cancellationToken);
        return member;
    }
}

internal static class RandomString
{
    private static readonly Random random = new();

    public static string Generate(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
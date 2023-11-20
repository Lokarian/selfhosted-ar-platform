using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Entities.Video;
using CoreServer.Domain.Events.Video;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Commands.JoinVideoSession;

public class JoinVideoSessionCommand : IRequest<VideoMember>
{
    public Guid VideoSessionId { get; set; }
}

public class JoinVideoSessionCommandHandler : IRequestHandler<JoinVideoSessionCommand, VideoMember>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public JoinVideoSessionCommandHandler(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<VideoMember> Handle(JoinVideoSessionCommand request, CancellationToken cancellationToken)
    {
        var videoSession = await _context.VideoSessions
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.BaseSessionId == request.VideoSessionId, cancellationToken);

        if (videoSession == null)
        {
            throw new NotFoundException(nameof(VideoSession), request.VideoSessionId);
        }

        var baseMember = await _context.SessionMembers
            .FirstOrDefaultAsync(x => x.UserId == _currentUserService.User!.Id, cancellationToken);
        if (baseMember == null)
        {
            throw new NotFoundException(nameof(SessionMember), _currentUserService.User!.Id);
        }

        if (_currentUserService.Connection == null)
        {
            throw new NotFoundException(nameof(UserConnection), _currentUserService.User!.Id);
        }
        var member = new VideoMember()
        {
            BaseMember = baseMember,
            Session = videoSession,
            AccessKey = RandomString.Generate(10),
            UserConnectionId = _currentUserService.Connection.Id,
        };
        videoSession.Members.Add(member);
        member.AddDomainEvent(new VideoMemberUpdatedEvent(member));
        _context.VideoMembers.Add(member);
        await _context.SaveChangesAsync(cancellationToken);
        return member;
    }
}

static class RandomString
{
    private static Random random = new Random();

    public static string Generate(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
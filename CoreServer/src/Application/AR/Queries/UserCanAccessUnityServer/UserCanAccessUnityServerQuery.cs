using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.AR;
using MediatR;

namespace CoreServer.Application.AR.Queries.UserCanAccessUnityServer;

public record UserCanAccessUnityServerQuery : IRequest<bool>
{
    public Guid ArSessionId { get; set; }
    public Guid MemberId { get; set; }
    public ArUserRole Role { get; set; }
}

public class UserCanAccessUnityServerQueryHandler : IRequestHandler<UserCanAccessUnityServerQuery, bool>
{
    private readonly IApplicationDbContext _context;

    public UserCanAccessUnityServerQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UserCanAccessUnityServerQuery request, CancellationToken cancellationToken)
    {
        ArMember? member = await _context.ArMembers.FindAsync(request.MemberId);
        if (member == null)
        {
            return false;
        }

        if (member.SessionId != request.ArSessionId)
        {
            return false;
        }

        if (member.Role != request.Role)
        {
            return false;
        }

        return true;
    }
}
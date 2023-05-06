using CoreServer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Queries;

//from:
/*{
#   "ip": "ip",
#   "user": "user",
#   "password": "password",
#   "path": "path",
#   "protocol": "rtsp|rtmp|hls|webrtc",
#   "id": "id",
#   "action": "read|publish",
#   "query": "query"
# }*/
public record UserCanAccessStreamQuery : IRequest<bool>
{
    public String Ip { get; init; } = null!;
    public String User { get; init; } = null!;
    public String Password { get; init; } = null!;
    public String Path { get; init; } = null!;
    public String Protocol { get; init; } = null!;
    public String Id { get; init; } = null!;
    public String Action { get; init; } = null!;
    public String Query { get; init; } = null!;
}

public class UserCanAccessStreamQueryHandler : IRequestHandler<UserCanAccessStreamQuery, bool>
{
    private readonly IApplicationDbContext _context;

    public UserCanAccessStreamQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UserCanAccessStreamQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Path.Split("/")[0], out var streamId))
        {
            return false;
        }

        var stream = await _context.VideoStreams
            .Include(v => v.Owner)
            .ThenInclude(o => o.Session)
            .Where(s => s.Id == streamId).FirstOrDefaultAsync(cancellationToken);
        if (stream == null)
        {
            return false;
        }

        if (!Guid.TryParse(request.User, out var userId))
        {
            return false;
        }

        switch (request.Action)
        {
            case "read":
                return stream.Owner.Session.Members.Any(m => m.BaseMember.UserId == userId);
            case "publish":
                return stream.OwnerId == userId;
            default:
                return false;
        }
    }
}
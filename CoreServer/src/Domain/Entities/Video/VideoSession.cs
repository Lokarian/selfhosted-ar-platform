using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Entities.Video;

namespace CoreServer.Domain.Entities.Chat;

public class VideoSession:EntityWithEvents
{
    public BaseSession BaseSession { get; set; } = null!;
    public Guid BaseSessionId { get; set; }
    public IList<VideoMember> Members { get; init; } = new List<VideoMember>();
}
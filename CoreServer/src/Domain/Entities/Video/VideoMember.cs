using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;

namespace CoreServer.Domain.Entities.Video;

public class VideoMember : EntityWithEvents
{
    public Guid Id { get; set; }
    public SessionMember BaseMember { get; set; } = null!;
    public Guid BaseMemberId { get; set; }

    public UserConnection UserConnection { get; set; } = null!;
    public Guid UserConnectionId { get; set; }
    public VideoSession Session { get; set; } = null!;
    public Guid SessionId { get; set; }

    public IList<VideoStream> Streams { get; set; } = new List<VideoStream>();
    public string? AccessKey { get; set; }
    public DateTime? DeletedAt { get; set; }
}
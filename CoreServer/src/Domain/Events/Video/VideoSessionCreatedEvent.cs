using CoreServer.Domain.Entities.Chat;
using MediatR;

namespace CoreServer.Domain.Events.Video;

public class VideoSessionCreatedEvent : BaseEvent
{
    public VideoSession Session { get; set; } = null!;
    public VideoSessionCreatedEvent(VideoSession session)
    {
        Session = session;
    }
}
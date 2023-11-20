using CoreServer.Domain.Entities.Video;

namespace CoreServer.Domain.Events.Video;

public class VideoStreamUpdatedEvent : BaseEvent
{
    public VideoStreamUpdatedEvent(VideoStream stream)
    {
        Stream = stream;
    }

    public VideoStream Stream { get; }
}
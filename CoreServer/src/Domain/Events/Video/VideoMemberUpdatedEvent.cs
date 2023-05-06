using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Video;

namespace CoreServer.Domain.Events.Video;

public class VideoMemberUpdatedEvent : BaseEvent
{
    public VideoMemberUpdatedEvent(VideoMember videoMember)
    {
        VideoMember = videoMember;
    }

    public VideoMember VideoMember { get; }
}
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.AR;

namespace CoreServer.Domain.Events.Ar;

public class ArMemberUpdatedEvent : BaseEvent
{
    public ArMemberUpdatedEvent(ArMember arMember)
    {
        ArMember = arMember;
    }

    public ArMember ArMember { get; }
}
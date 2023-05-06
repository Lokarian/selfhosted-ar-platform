using CoreServer.Domain.Entities.Session;

namespace CoreServer.Domain.Events.Session;

public class SessionMemberUpdatedEvent : BaseEvent
{
    public SessionMemberUpdatedEvent(SessionMember sessionMember)
    {
        SessionMember = sessionMember;
    }

    public SessionMember SessionMember { get; }
}
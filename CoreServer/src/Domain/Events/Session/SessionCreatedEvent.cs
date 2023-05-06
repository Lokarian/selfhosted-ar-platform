using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;

namespace CoreServer.Domain.Events.Session;

public class SessionCreatedEvent : BaseEvent
{
    public SessionCreatedEvent(UserSession session)
    {
        Session = session;
    }

    public UserSession Session { get; }
}
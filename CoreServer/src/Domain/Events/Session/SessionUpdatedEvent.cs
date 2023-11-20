using CoreServer.Domain.Entities.Session;

namespace CoreServer.Domain.Events.Session;

public class SessionUpdatedEvent : BaseEvent
{
    public SessionUpdatedEvent(BaseSession session, IList<AppUser> removedUsers)
    {
        Session = session;
        RemovedUsers = removedUsers;
    }

    public BaseSession Session { get; }
    public IList<AppUser>? RemovedUsers { get; }
}
using CoreServer.Domain.Entities.Session;

namespace CoreServer.Domain.Events.Session;

public class SessionUpdatedEvent : BaseEvent
{
    public SessionUpdatedEvent(UserSession session, IList<AppUser> removedUsers)
    {
        Session = session;
        RemovedUsers = removedUsers;
    }

    public UserSession Session { get; }
    public IList<AppUser>? RemovedUsers { get; }
}
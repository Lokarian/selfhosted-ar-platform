using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Domain.Events.Chat;

public class ChatSessionUpdatedEvent : BaseEvent
{
    public ChatSessionUpdatedEvent(ChatSession session, IList<AppUser> removedUsers)
    {
        Session = session;
        RemovedUsers = removedUsers;
    }

    public ChatSession Session { get; }
    public IList<AppUser> RemovedUsers { get; }
}
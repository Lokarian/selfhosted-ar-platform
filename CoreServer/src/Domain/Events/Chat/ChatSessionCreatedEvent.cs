using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Domain.Events.Chat;

public class ChatSessionCreatedEvent : BaseEvent
{
    public ChatSessionCreatedEvent(ChatSession session)
    {
        Session = session;
    }

    public ChatSession Session { get; }
}
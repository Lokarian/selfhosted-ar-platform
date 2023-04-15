using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Domain.Events;

public class ChatSessionCreatedEvent : BaseEvent
{
    public ChatSessionCreatedEvent(ChatSession session)
    {
        Session = session;
    }

    public ChatSession Session { get; }
}
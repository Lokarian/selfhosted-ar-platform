using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Domain.Events.Chat;

public class ChatSessionCreatedEvent : BaseEvent
{
    public ChatSession Session { get; }

    public ChatSessionCreatedEvent(ChatSession session)
    {
        Session = session;
    }
}
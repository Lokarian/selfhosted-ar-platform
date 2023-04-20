using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Domain.Events.Chat;

public class ChatMassageCreatedEvent : BaseEvent
{
    public ChatMassageCreatedEvent(ChatMessage message)
    {
        Message = message;
    }

    public ChatMessage Message { get; }
}
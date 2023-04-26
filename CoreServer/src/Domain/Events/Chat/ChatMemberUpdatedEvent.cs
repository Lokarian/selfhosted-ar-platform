using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Domain.Events.Chat;

public class ChatMemberUpdatedEvent : BaseEvent
{
    public ChatMemberUpdatedEvent(ChatMember chatMember)
    {
        ChatMember = chatMember;
    }

    public ChatMember ChatMember { get; }
}
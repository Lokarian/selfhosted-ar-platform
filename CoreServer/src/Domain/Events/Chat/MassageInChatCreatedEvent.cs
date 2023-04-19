﻿using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Domain.Events.Chat;

public class MassageInChatCreatedEvent : BaseEvent
{
    public MassageInChatCreatedEvent(ChatMessage message)
    {
        Message = message;
    }

    public ChatMessage Message { get; }
}
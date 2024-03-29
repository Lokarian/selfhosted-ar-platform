﻿using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Domain.Events.Chat;

public class ChatMessageDeletedEvent : BaseEvent
{
    public ChatMessageDeletedEvent(ChatMessage message)
    {
        Message = message;
    }

    public ChatMessage Message { get; }
}
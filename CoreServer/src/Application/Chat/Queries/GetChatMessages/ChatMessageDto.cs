using AutoMapper;
using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Application.Chat.Queries.GetChatMessages;

public class ChatMessageDto : IMapFrom<ChatMessage>
{
    public Guid Id { get; set; }
    public string Text { get; set; } = null!;
    public DateTime SentAt { get; set; }
    public Guid SenderId { get; set; }
    public Guid SessionId { get; set; }
    
}
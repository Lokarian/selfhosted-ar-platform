namespace CoreServer.Domain.Entities.Chat;

public class ChatSession : BaseEntity
{
    public IList<ChatMessage> Messages { get; init; } = new List<ChatMessage>();
    public IList<ChatMember> Members { get; init; } = new List<ChatMember>();
    public string? Name { get; set; }
}
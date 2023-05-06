using CoreServer.Domain.Entities.Session;

namespace CoreServer.Domain.Entities.Chat;

public class ChatSession:EntityWithEvents
{
    public UserSession BaseSession { get; set; } = null!;
    public Guid BaseSessionId { get; set; }
    public IList<ChatMessage> Messages { get; init; } = new List<ChatMessage>();
    public IList<ChatMember> Members { get; init; } = new List<ChatMember>();

}
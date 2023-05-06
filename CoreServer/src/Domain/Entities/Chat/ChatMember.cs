using CoreServer.Domain.Entities.Session;

namespace CoreServer.Domain.Entities.Chat;

public class ChatMember : EntityWithEvents
{
    public SessionMember BaseMember { get; set; } = null!;
    public Guid BaseMemberId { get; set; }

    public ChatSession Session { get; set; } = null!;
    public Guid SessionId { get; set; }
    public DateTime? LastSeen { get; set; }
}
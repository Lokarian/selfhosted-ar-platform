namespace CoreServer.Domain.Entities.Chat;

public class ChatMember
{
    public Guid SessionId { get; set; }
    public ChatSession Session { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateTime? LastSeen { get; set; }

    public ChatMember(AppUser user, ChatSession session)
    {
        User = user;
        Session = session;
    }

    public ChatMember()
    {
    }
}
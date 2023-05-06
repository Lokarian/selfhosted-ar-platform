namespace CoreServer.Domain.Entities.Session;

public class SessionMember : BaseEntity
{
    public SessionMember(AppUser user, UserSession session)
    {
        User = user;
        Session = session;
    }

    public SessionMember()
    {
    }

    public UserSession Session { get; set; } = null!;
    public Guid SessionId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid UserId { get; set; }
    public DateTime? DeletedAt { get; set; }
}
using CoreServer.Domain.Entities.Session;

namespace CoreServer.Domain.Entities.AR;

public class ArMember : EntityWithEvents
{
    public Guid Id { get; set; }
    public SessionMember BaseMember { get; set; } = null!;
    public Guid BaseMemberId { get; set; }


    public UserConnection UserConnection { get; set; } = null!;
    public Guid UserConnectionId { get; set; }

    public ArSession Session { get; set; } = null!;
    public Guid SessionId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ArUserRole Role { get; set; }
    public string? AccessKey { get; set; } = null!;
}

public enum ArUserRole
{
    ArUser,
    Spectator,
    Server
}
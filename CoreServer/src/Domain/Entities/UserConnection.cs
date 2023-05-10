namespace CoreServer.Domain.Entities;

public class UserConnection:BaseEntity
{
    public string ConnectionId { get; set; } = null!;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateTime? DisconnectedAt { get; set; }
}
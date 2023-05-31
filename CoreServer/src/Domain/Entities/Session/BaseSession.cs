using CoreServer.Domain.Entities.AR;
using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Domain.Entities.Session;

public class BaseSession : BaseAuditableEntity
{
    public IList<SessionMember> Members { get; init; } = new List<SessionMember>();
    public string? Name { get; set; }
    
    public VideoSession? VideoSession { get; set; }
    public ChatSession? ChatSession { get; set; }
    public ArSession? ArSession { get; set; }
    
}
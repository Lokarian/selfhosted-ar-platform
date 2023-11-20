using CoreServer.Domain.Entities.Session;

namespace CoreServer.Domain.Entities.AR;

public class ArSession:EntityWithEvents
{
    
    public BaseSession BaseSession { get; set; } = null!;
    public Guid BaseSessionId { get; set; }
    public IList<ArMember> Members { get; init; } = new List<ArMember>();
    public ArSessionType SessionType { get; set; }
    public ArServerState ServerState { get; set; }
}

public enum ArSessionType
{
    RemoteAssist,
}
public enum ArServerState
{
    Stopped,
    Starting,
    Running
}
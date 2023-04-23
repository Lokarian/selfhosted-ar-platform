namespace CoreServer.Domain.Events.User;

public class NewUserConnectionEvent : BaseEvent
{
    public NewUserConnectionEvent(Guid userId, string connectionId)
    {
        UserId = userId;
        ConnectionId = connectionId;
    }

    public Guid UserId { get; }
    public string ConnectionId { get; }
}
namespace CoreServer.Domain.Events.User;

public class UserConnectionDisconnectedEvent : BaseEvent
{
    public UserConnectionDisconnectedEvent(Guid userId, string connectionId)
    {
        UserId = userId;
        ConnectionId = connectionId;
    }

    public Guid UserId { get; }
    public string ConnectionId { get; }
}
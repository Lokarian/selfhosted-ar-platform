namespace CoreServer.Domain.Events.User;

public class UserConnectionDisconnectedEvent : BaseEvent
{
    public UserConnectionDisconnectedEvent(UserConnection userConnection)
    {
        UserConnection = userConnection;
    }
    
    public UserConnection UserConnection { get; set; } = null!;
}
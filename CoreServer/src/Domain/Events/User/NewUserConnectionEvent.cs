namespace CoreServer.Domain.Events.User;

public class NewUserConnectionEvent : BaseEvent
{
    public NewUserConnectionEvent(UserConnection userConnection)
    {
        UserConnection = userConnection;
    }

    public UserConnection UserConnection { get; set; } = null!;
}
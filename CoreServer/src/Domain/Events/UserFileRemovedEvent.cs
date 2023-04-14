namespace CoreServer.Domain.Events;

public class UserFileRemovedEvent : BaseEvent
{
    public UserFileRemovedEvent(UserFile userFile)
    {
        UserFile = userFile;
    }

    public UserFile UserFile { get; }
}
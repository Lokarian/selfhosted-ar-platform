using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.AR;

namespace CoreServer.Domain.Events.Ar;

public class ArSessionUpdatedEvent : BaseEvent
{
    public ArSessionUpdatedEvent(ArSession arSession)
    {
        ArSession = arSession;
    }

    public ArSession ArSession { get; }
}
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.AR;

namespace CoreServer.Domain.Events.Ar;

public class ArSessionCreatedEvent : BaseEvent
{
    public ArSessionCreatedEvent(ArSession arSession)
    {
        ArSession = arSession;
    }

    public ArSession ArSession { get; }
}
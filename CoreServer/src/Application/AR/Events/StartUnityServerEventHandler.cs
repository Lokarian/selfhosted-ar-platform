using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Events.Ar;
using MediatR;

namespace CoreServer.Application.AR.Events;

public class StartUnityServerEventHandler : INotificationHandler<ArSessionCreatedEvent>
{
    private readonly IUnityServerService _unityServerService;

    public StartUnityServerEventHandler(IUnityServerService unityServerService)
    {
        _unityServerService = unityServerService;
    }

    public async Task Handle(ArSessionCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _unityServerService.StartServer(notification.ArSession.BaseSessionId, notification.ArSession.SessionType);
    }
}
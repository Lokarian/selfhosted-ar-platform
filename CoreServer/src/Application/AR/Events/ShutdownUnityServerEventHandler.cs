using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Events.Ar;
using MediatR;

namespace CoreServer.Application.AR.Events;

public class ShutdownUnityServerEventHandler : INotificationHandler<ArSessionUpdatedEvent>
{
    private readonly IUnityServerService _unityServerService;

    public ShutdownUnityServerEventHandler(IUnityServerService unityServerService)
    {
        _unityServerService = unityServerService;
    }

    public async Task Handle(ArSessionUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await _unityServerService.ShutdownServer(notification.ArSession.BaseSessionId);
    }
}
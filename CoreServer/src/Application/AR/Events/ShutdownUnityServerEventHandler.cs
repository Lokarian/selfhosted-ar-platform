using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.AR;
using CoreServer.Domain.Events.Ar;
using MediatR;

namespace CoreServer.Application.AR.Events;

public class ShutdownUnityServerEventHandler : INotificationHandler<ArSessionUpdatedEvent>
{
    private readonly IUnityServerService _unityServerService;
    private readonly IApplicationDbContext _context;

    public ShutdownUnityServerEventHandler(IUnityServerService unityServerService, IApplicationDbContext context)
    {
        _unityServerService = unityServerService;
        _context = context;
    }

    public async Task Handle(ArSessionUpdatedEvent notification, CancellationToken cancellationToken)
    {
        //if (notification.ArSession.ServerState == ArServerState.Stopped)
        if (_context.ArSessions.Any(x =>
                x.BaseSessionId == notification.ArSession.BaseSessionId && x.ServerState == ArServerState.Stopped))
        {
            await _unityServerService.ShutdownServer(notification.ArSession.BaseSessionId);
        }
    }
}
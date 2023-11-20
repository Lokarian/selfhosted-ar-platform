using CoreServer.Application.AR.Commands.StartArServer;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Events.Ar;
using MediatR;

namespace CoreServer.Application.AR.Events;

public class StartUnityServerEventHandler : INotificationHandler<ArSessionCreatedEvent>
{
    private readonly IMediator _mediator;

    public StartUnityServerEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(ArSessionCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _mediator.Send(new StartArServerCommand() { ArSessionId = notification.ArSession.BaseSessionId },
            cancellationToken);
    }
}
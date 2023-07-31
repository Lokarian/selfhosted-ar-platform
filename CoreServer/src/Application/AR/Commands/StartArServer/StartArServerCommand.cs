using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.AR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Commands.StartArServer;

public record StartArServerCommand : IRequest
{
    public Guid ArSessionId { get; set; }
}

public class StartArServerCommandHandler : IRequestHandler<StartArServerCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnityServerService _unityServerService;
    private readonly IMediator _mediator;

    public StartArServerCommandHandler(IApplicationDbContext context, IMediator mediator,
        IUnityServerService unityServerService)
    {
        _context = context;
        _mediator = mediator;
        _unityServerService = unityServerService;
    }

    public async Task Handle(StartArServerCommand request, CancellationToken cancellationToken)
    {
        var arSession = await _context.ArSessions
            .FirstOrDefaultAsync(x => x.BaseSessionId == request.ArSessionId, cancellationToken);
        if (arSession == null)
        {
            throw new NotFoundException(nameof(ArSession), request.ArSessionId);
        }

        await _unityServerService.StartServer(arSession.BaseSessionId, arSession.SessionType);
        return;
    }
}
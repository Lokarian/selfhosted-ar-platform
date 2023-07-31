using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Events.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.User.Commands.UpdateUserConnection;

public record DisconnectUserConnectionCommand : IRequest
{
    public string ConnectionId { get; init; }
}

public class DisconnectUserConnectionCommandHandler : IRequestHandler<DisconnectUserConnectionCommand>
{
    private readonly IApplicationDbContext _context;

    public DisconnectUserConnectionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DisconnectUserConnectionCommand request, CancellationToken cancellationToken)
    {
        var userConnection = await _context.UserConnections.AsTracking().FirstOrDefaultAsync(
            uc => uc.ConnectionId == request.ConnectionId, cancellationToken);
        if (userConnection is null)
        {
            //connection stopped before it was initialized
            return;
        }
        userConnection.DisconnectedAt = DateTime.UtcNow;
        userConnection.AddDomainEvent(new UserConnectionDisconnectedEvent(userConnection));
        
        await _context.SaveChangesAsync(cancellationToken);

        return;
    }
}
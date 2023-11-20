using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Events.User;
using MediatR;

namespace CoreServer.Application.User.Commands.CreateUserConnection;

public record CreateUserConnectionCommand : IRequest<UserConnection>
{
    public string ConnectionId { get; init; }
    public Guid UserId { get; init; }
}

public class CreateUserConnectionCommandHandler : IRequestHandler<CreateUserConnectionCommand,UserConnection>
{
    private readonly IApplicationDbContext _context;

    public CreateUserConnectionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserConnection> Handle(CreateUserConnectionCommand request, CancellationToken cancellationToken)
    {
        var userConnection = new UserConnection
        {
            ConnectionId = request.ConnectionId,
            UserId = request.UserId
        };
        userConnection.AddDomainEvent(new NewUserConnectionEvent(userConnection));
        await _context.UserConnections.AddAsync(userConnection, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return userConnection;
    }
}
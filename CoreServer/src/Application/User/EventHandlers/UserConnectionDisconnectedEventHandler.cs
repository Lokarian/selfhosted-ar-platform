using AutoMapper;
using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Application.User.Queries;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Events.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.User.EventHandlers;

public class UserConnectionDisconnectedEventHandler : INotificationHandler<UserConnectionDisconnectedEvent>
{
    private readonly IUserConnectionStore _userConnectionStore;
    private readonly IUserProxy<IRpcUserService> _userProxy;
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UserConnectionDisconnectedEventHandler(IUserConnectionStore userConnectionStore,
        IUserProxy<IRpcUserService> userProxy, IApplicationDbContext context, IMapper mapper)
    {
        _userConnectionStore = userConnectionStore;
        _userProxy = userProxy;
        _context = context;
        _mapper = mapper;
    }

    public async Task Handle(UserConnectionDisconnectedEvent notification, CancellationToken cancellationToken)
    {
        var connections = await this._context.UserConnections
            .Where(uc => uc.UserId == notification.UserConnection.UserId && uc.DisconnectedAt == null)
            .ToListAsync(cancellationToken);
        await _userConnectionStore.RemoveConnection(notification.UserConnection.UserId,
            notification.UserConnection.ConnectionId);
        if (connections.Count == 0)
        {
            var user = await _context.AppUsers.AsTracking()
                .FirstOrDefaultAsync(u => u.Id == notification.UserConnection.UserId, cancellationToken);
            if (user is null)
            {
                throw new NotFoundException(nameof(AppUser));
            }

            user.OnlineStatus = OnlineStatus.Offline;
            await _context.SaveChangesAsync(cancellationToken);
            await (await _userProxy.All()).UpdateUser(_mapper.Map<AppUserDto>(user));
        }
    }
}
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
using Microsoft.Extensions.DependencyInjection;

namespace CoreServer.Application.User.EventHandlers;

public class UserConnectionDisconnectedEventHandler : INotificationHandler<UserConnectionDisconnectedEvent>
{
    private readonly IUserConnectionStore _userConnectionStore;
    private readonly IUserProxy<IRpcUserService> _userProxy;
    private IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public UserConnectionDisconnectedEventHandler(IUserConnectionStore userConnectionStore,
        IUserProxy<IRpcUserService> userProxy, IMapper mapper, IServiceScopeFactory serviceScopeFactory)
    {
        _userConnectionStore = userConnectionStore;
        _userProxy = userProxy;
        _mapper = mapper;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Handle(UserConnectionDisconnectedEvent notification, CancellationToken cancellationToken)
    {
        //create new db context to avoid tracking issues
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            _context = scope.ServiceProvider.GetService<IApplicationDbContext>();
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
}
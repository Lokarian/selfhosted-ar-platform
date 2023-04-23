using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Application.User.Queries;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Events.User;
using MediatR;

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
        var connections = await _userConnectionStore.GetConnections(notification.UserId);
        if (!connections.Any())
        {
            var user = await _context.AppUsers.FindAsync(notification.UserId);
            user.OnlineStatus = OnlineStatus.Offline;
            await _context.SaveChangesAsync(cancellationToken);
            await (await _userProxy.All()).UpdateUser(_mapper.Map<AppUserDto>(user));
        }
    }
}
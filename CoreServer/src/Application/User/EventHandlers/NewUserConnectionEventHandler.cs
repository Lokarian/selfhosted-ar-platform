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

public class NewUserConnectionEventHandler : INotificationHandler<NewUserConnectionEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserConnectionStore _userConnectionStore;
    private readonly IUserProxy<IRpcUserService> _userProxy;
    private readonly IMapper _mapper;


    public NewUserConnectionEventHandler(IApplicationDbContext context, IUserConnectionStore userConnectionStore,
        IUserProxy<IRpcUserService> userProxy, IMapper mapper)
    {
        _context = context;
        _userConnectionStore = userConnectionStore;
        _userProxy = userProxy;
        _mapper = mapper;
    }

    public async Task Handle(NewUserConnectionEvent notification, CancellationToken cancellationToken)
    {
        // set online status and add connectionId to IUserConnectionStore
        var user = await _context.AppUsers.AsTracking()
            .FirstOrDefaultAsync(u => u.Id == notification.UserConnection.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException(nameof(AppUser));
        }

        user.OnlineStatus = OnlineStatus.Online;
        await _userConnectionStore.AddConnection(notification.UserConnection.UserId,
            notification.UserConnection.ConnectionId);
        await _context.SaveChangesAsync(cancellationToken);
        await (await _userProxy.All()).UpdateUser(_mapper.Map<AppUserDto>(user));
    }
}
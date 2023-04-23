using CoreServer.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CoreServer.Infrastructure.RPC;

[Authorize]
public class SignalRHub : Hub
{
    private readonly ILogger<SignalRHub> _logger;
    private readonly IUserConnectionStore _userConnectionStore;

    public SignalRHub(ILogger<SignalRHub> logger, IUserConnectionStore userConnectionStore)
    {
        _logger = logger;
        _userConnectionStore = userConnectionStore;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation($"User ${Context.UserIdentifier} on Client {Context.ConnectionId} connected");
        _userConnectionStore.AddConnection(Guid.Parse(Context.UserIdentifier!), Context.ConnectionId);
        
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation($"User ${Context.UserIdentifier} on Client {Context.ConnectionId} disconnected");
        _userConnectionStore.RemoveConnection(Guid.Parse(Context.UserIdentifier!), Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public Task RegisterService(string serviceName)
    {
        _logger.LogInformation(
            $"User ${Context.UserIdentifier} on Client {Context.ConnectionId} registered service {serviceName}");
        _userConnectionStore.AddServiceToConnection(Context.ConnectionId, serviceName);
        return Task.CompletedTask;
    }
}
using System.Security.Claims;
using CoreServer.Application.Chat.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CoreServer.Infrastructure.RPC;

public class SignalRHub : Hub
{
    private readonly ILogger<SignalRHub> _logger;

    public SignalRHub(ILogger<SignalRHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        string name = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation($"Client {name} connected");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        string name = Context.User.Identity.Name;
        _logger.LogInformation($"Client {name} disconnected");
        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("UpdateChatSession", new ChatSessionDto() { Name = "test" });
    }

    public async Task RegisterService(string serviceName)
    {
        _logger.LogInformation(
            $"User ${Context.UserIdentifier} on Client {Context.ConnectionId} registered service {serviceName}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{Context.UserIdentifier}-{serviceName}");
    }
}
using System.Collections.Concurrent;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Events.User;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CoreServer.Infrastructure.Services;

public class UserConnectionStore : IUserConnectionStore
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _services = new();
    private readonly IServiceProvider _serviceProvider;

    public UserConnectionStore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task AddConnection(Guid userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            connections.Add(connectionId);
        }
        else
        {
            _connections.TryAdd(userId, new HashSet<string> { connectionId });
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(new NewUserConnectionEvent(userId, connectionId));
        }
    }

    public async Task RemoveConnection(Guid userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            connections.Remove(connectionId);
        }
        _services.TryRemove(connectionId, out _);

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(new UserConnectionDisconnectedEvent(userId, connectionId));
    }

    public Task<IEnumerable<string>> GetConnections(Guid userId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            return Task.FromResult(connections.AsEnumerable());
        }

        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task AddServiceToConnection(string connectionId, string serviceName)
    {
        if (_services.TryGetValue(connectionId, out var services))
        {
            services.Add(serviceName);
        }
        else
        {
            _services.TryAdd(connectionId, new HashSet<string> { serviceName });
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetConnectionsForService(string serviceName)
    {
        return Task.FromResult(_services.Where(s => s.Value.Contains(serviceName)).Select(s => s.Key));
    }

    public Task<IEnumerable<string>> GetConnectionsForUserService(Guid userId, string serviceName)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            return Task.FromResult(connections.Where(c =>
                _services.TryGetValue(c, out var services) && services.Contains(serviceName)));
        }

        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task<IEnumerable<string>> GetConnectionsForUserService(IEnumerable<Guid> userIds, string serviceName)
    {
        return Task.FromResult(_connections.Where(c => userIds.Contains(c.Key)).SelectMany(c =>
            c.Value.Where(v =>
                _services.TryGetValue(v, out var services) && services.Contains(serviceName))));
    }
}
using System.Reflection;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC.common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CoreServer.Infrastructure.RPC;

public class SignalRUserProxy<T> : IUserProxy<T> where T : class, IRpcService
{
    private readonly IHubContext<SignalRHub> _hubContext;
    private readonly IUserConnectionStore _userConnectionStore;
    private readonly ILogger<SignalRUserProxy<T>> _logger;

    public SignalRUserProxy(IHubContext<SignalRHub> hubContext, IUserConnectionStore userConnectionStore, ILogger<SignalRUserProxy<T>> logger)
    {
        _hubContext = hubContext;
        _userConnectionStore = userConnectionStore;
        _logger = logger;
    }

    public async Task<T> Client(Guid userId)
    {
        string interfaceName = typeof(T).Name.StartsWith("I") ? typeof(T).Name.Substring(1) : typeof(T).Name;
        var connectionIds = await _userConnectionStore.GetConnectionsForUserService(userId, interfaceName);
        _logger.LogInformation($"Creating Client proxy for {interfaceName} for {string.Join(", ", connectionIds)}");
        T proxy = SignalRDispatchProxy<T>.CreateProxy(_hubContext.Clients.Clients(connectionIds));
        return proxy;
    }

    public async Task<T> Clients(IEnumerable<Guid> userIds)
    {
        string interfaceName = typeof(T).Name.StartsWith("I") ? typeof(T).Name.Substring(1) : typeof(T).Name;
        var connectionIds = await _userConnectionStore.GetConnectionsForUserService(userIds, interfaceName);
        _logger.LogInformation($"Creating Clients proxy for {interfaceName} for {string.Join(", ", connectionIds)}");
        T proxy = SignalRDispatchProxy<T>.CreateProxy(_hubContext.Clients.Clients(connectionIds));
        return proxy;
    }

    public async Task<T> All()
    {
        string interfaceName = typeof(T).Name.StartsWith("I") ? typeof(T).Name.Substring(1) : typeof(T).Name;
        var connectionIds = await _userConnectionStore.GetConnectionsForService(interfaceName);
        _logger.LogInformation($"Creating All proxy for {interfaceName} for {string.Join(", ", connectionIds)}");
        T proxy = SignalRDispatchProxy<T>.CreateProxy(_hubContext.Clients.Clients(connectionIds));
        return proxy;
    }
}

internal class SignalRDispatchProxy<T> : DispatchProxy where T : class, IRpcService
{
    private string _serviceName;
    private IClientProxy _target;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        string endpointName = $"{_serviceName}/{targetMethod?.Name}";
        Task result = _target.SendCoreAsync(endpointName, args);
        return result;
    }

    public static T CreateProxy(IClientProxy target)
    {
        SignalRDispatchProxy<T>? proxy = Create<T, SignalRDispatchProxy<T>>() as SignalRDispatchProxy<T>;
        proxy._target = target;
        //remove the "I" from the interface name if it starts with one
        proxy._serviceName = typeof(T).Name.StartsWith("I") ? typeof(T).Name.Substring(1) : typeof(T).Name;
        return proxy as T;
    }
}
using System.Reflection;
using CoreServer.Application.RPC.common;
using Microsoft.AspNetCore.SignalR;

namespace CoreServer.Infrastructure.RPC;

public class SignalRUserProxy<T> : IUserProxy<T> where T : class, IRpcService
{
    private readonly IHubContext<SignalRHub> _hubContext;

    public SignalRUserProxy(IHubContext<SignalRHub> hubContext)
    {
        _hubContext = hubContext;
        Console.WriteLine("new SignalRUserProxy");
    }

    public T Client(Guid userId)
    {
        //get name of interface without the "I" if it starts with one
        var interfaceName = typeof(T).Name.StartsWith("I") ? typeof(T).Name.Substring(1) : typeof(T).Name;
        T proxy = SignalRDispatchProxy<T>.CreateProxy(_hubContext.Clients.Group($"{interfaceName}-{userId}"));
        return proxy;
    }

    public T Clients(IEnumerable<Guid> userIds)
    {
        var interfaceName = typeof(T).Name.StartsWith("I") ? typeof(T).Name.Substring(1) : typeof(T).Name;
        T proxy = SignalRDispatchProxy<T>.CreateProxy(_hubContext.Clients.Groups(userIds.Select(x => $"{interfaceName}-{x}")));
        return proxy;
    }
}

class SignalRDispatchProxy<T> : DispatchProxy where T : class, IRpcService
{
    private IClientProxy _target;
    private string _serviceName;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var endpointName = $"{_serviceName}/{targetMethod?.Name}";
        Console.WriteLine(endpointName);
        var result = _target.SendCoreAsync(endpointName, args);
        return result;
    }

    public static T CreateProxy(IClientProxy target)
    {
        var proxy = Create<T, SignalRDispatchProxy<T>>() as SignalRDispatchProxy<T>;
        proxy._target = target;
        //remove the "I" from the interface name if it starts with one
        proxy._serviceName = typeof(T).Name.StartsWith("I") ? typeof(T).Name.Substring(1) : typeof(T).Name;
        return proxy as T;
    }
}
using System.Reflection;
using CoreServer.Application.RPCInterfaces;
using Microsoft.AspNetCore.SignalR;

namespace CoreServer.Infrastructure.RPC;

public class SignalRUserProxy<T> : IUserProxy<T> where T : IRpcService
{
    private readonly IHubContext<SignalRHub, IRpcFusionInterface> _hubContext;
    private readonly SignalRDispatchProxy<IRpcService> _dispatchProxy;
    public SignalRUserProxy(IHubContext<SignalRHub, IRpcFusionInterface> hubContext)
    {
        _hubContext = hubContext;
        _dispatchProxy = (SignalRDispatchProxy<IRpcService>)DispatchProxy.Create<IRpcService, SignalRDispatchProxy<IRpcService>>();
        Console.WriteLine("new SignalRUserProxy");
    }
    private string GetNameOfGenericTypeT()
    {
        return typeof(T).Name;
    }
    public T Client(Guid userId)
    {
        Console.WriteLine(GetNameOfGenericTypeT());
        var proxy = SignalRDispatchProxy<T>.Create(_hubContext.Clients.User(userId.ToString()));
        return proxy;
        //return (T)_hubContext.Clients.User(userId.ToString());
    }

    public T Clients(IEnumerable<Guid> userIds)
    {
        return (T)_hubContext.Clients.Users(userIds.Select(x => x.ToString()));
    }
}

class SignalRDispatchProxy<T> : DispatchProxy where T : IRpcService
{
    private IRpcService Target { get; set; }
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        Console.WriteLine(targetMethod?.Name);
        var result = targetMethod.Invoke(Target, args);
        return result;
    }
    public static T Create(IRpcService target)
    {
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        object proxy = Create<T, SignalRDispatchProxy<T>>();
        ((SignalRDispatchProxy<T>)proxy).Target = target;
        return (T)proxy;
    }
}
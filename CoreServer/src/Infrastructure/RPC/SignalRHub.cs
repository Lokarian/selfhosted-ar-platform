using CoreServer.Application.RPCInterfaces;
using Microsoft.AspNetCore.SignalR;

namespace CoreServer.Infrastructure.RPC;

public class SignalRHub: Hub<RPCWebClient>
{
    
}
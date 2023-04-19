namespace CoreServer.Application.RPC.common;

public interface IUserProxy<out T> where T : IRpcService
{
    T Client(Guid userId);
    
    T Clients(IEnumerable<Guid> userIds);
}
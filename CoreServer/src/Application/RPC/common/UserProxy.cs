namespace CoreServer.Application.RPC.common;

public interface IUserProxy<T> where T : IRpcService
{
    Task<T> Client(Guid userId);

    Task<T> Clients(IEnumerable<Guid> userIds);
    
    Task<T> All();
}
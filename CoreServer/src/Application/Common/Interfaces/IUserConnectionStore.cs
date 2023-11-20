namespace CoreServer.Application.Common.Interfaces;

public interface IUserConnectionStore
{
    public Task AddConnection(Guid userId, string connectionId);
    public Task RemoveConnection(Guid userId, string connectionId);
    public Task<IEnumerable<string>> GetConnections(Guid userId);
    public Task AddServiceToConnection(string connectionId, string serviceName);
    public Task<IEnumerable<string>> GetConnectionsForService(string serviceName);
    public Task<IEnumerable<string>> GetConnectionsForUserService(Guid userId, string serviceName);
    public Task<IEnumerable<string>> GetConnectionsForUserService(IEnumerable<Guid> userIds, string serviceName);
    
}
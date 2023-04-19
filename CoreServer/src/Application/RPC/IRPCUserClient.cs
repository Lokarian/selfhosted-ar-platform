using CoreServer.Application.User.Queries;

namespace CoreServer.Application.RPCInterfaces;

public interface IRpcUserClient:IRpcService
{
    Task UpdateUser(AppUserDto user);
}
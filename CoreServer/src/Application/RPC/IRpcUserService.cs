using CoreServer.Application.RPC.common;
using CoreServer.Application.User.Queries;

namespace CoreServer.Application.RPC;

public interface IRpcUserService : IRpcService
{
    Task UpdateUser(AppUserDto user);
    Task UpdateConnectionToken(string token);
}
using CoreServer.Domain.Entities.AR;

namespace CoreServer.Application.Common.Interfaces;

public interface IUnityServerService
{
    public Task StartServer(Guid arSessionId,ArSessionType sessionType);
    public Task ShutdownServer(Guid arSessionId);
}
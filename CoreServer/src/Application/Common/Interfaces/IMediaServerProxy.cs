namespace CoreServer.Application.Common.Interfaces;

public interface IMediaServerProxy
{
    Task OpenStream(Guid streamId, Guid publisherId,IEnumerable<Guid> subscriberIds);
    Task CloseStream(Guid streamId);
    Task AddSubscriber(Guid streamId, Guid subscriberId);
    Task RemoveSubscriber(Guid streamId, Guid subscriberId);
}
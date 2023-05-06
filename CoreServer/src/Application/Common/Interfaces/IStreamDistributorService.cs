using System.Threading.Channels;

namespace CoreServer.Application.Common.Interfaces;

public interface IStreamDistributorService<T>
{
    Task<Guid> RegisterStream(Guid clientId, ChannelReader<T> stream, Guid? streamId = null);
    Task RemoveStream(Guid streamId);
    Task<Guid> Subscribe(Guid streamId, Guid clientId, ChannelWriter<T> observer, CancellationToken cancellationToken);
    void Unsubscribe(Guid subscriptionId);
    void Unsubscribe(Guid streamId, Guid clientId);
}
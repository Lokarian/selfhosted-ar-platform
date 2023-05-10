using System.Threading.Channels;

namespace CoreServer.Application.Common.Interfaces;

public interface IStreamDistributorService<T>
{
    Task<Guid> RegisterStream(Guid clientId, ChannelReader<T> stream, string topic);
    Task RemoveStream(Guid streamId);
    Task StopStream(Guid streamId);
    Task StopTopic(string topic);
    Task<Guid> Subscribe(string topic, Guid clientId, ChannelWriter<T> observer, CancellationToken cancellationToken);
    void Unsubscribe(Guid subscriptionId);
    void Unsubscribe(string topic, Guid clientId);
}
using System.Threading.Channels;

namespace CoreServer.Application.Common.Interfaces;

public enum StreamMetaEventType
{
    SubscriberConnected,
    SubscriberDisconnected,
    PublisherConnected,
    PublisherDisconnected,
}
public struct StreamMetaEvent
{
    public StreamMetaEventType Type { get; set; }
    public string Topic { get; set; }
    public Guid ClientId { get; set; }
}


public interface IStreamDistributorService<T>
{
    Task<Guid> RegisterStream(Guid clientId, ChannelReader<T> stream, string topic);
    Task RemoveStream(Guid streamId);
    Task StopStream(Guid streamId);
    Task StopTopic(string topic);
    Task<Guid> Subscribe(string topic, Guid clientId, ChannelWriter<T> observer, CancellationToken cancellationToken);
    void Unsubscribe(Guid subscriptionId);
    void Unsubscribe(string topic, Guid clientId);
    public Task Publish(T value, string Topic);

}
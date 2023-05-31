using System.Collections.Concurrent;
using System.Threading.Channels;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;

namespace CoreServer.Infrastructure.RPC;

public class StreamDistributorService<T> : IStreamDistributorService<T>
{
    private readonly IUserProxy<IRpcStreamingService> _userProxy;

    public StreamDistributorService(IUserProxy<IRpcStreamingService> userProxy)
    {
        _userProxy = userProxy;
    }

    //storage for streams and linking with clientIds
    private readonly IDictionary<Guid, ChannelReader<T>> _streams =
        new ConcurrentDictionary<Guid, ChannelReader<T>>();

    private readonly IDictionary<Guid, List<Guid>> _clientToStreams = new ConcurrentDictionary<Guid, List<Guid>>();

    private readonly IDictionary<Guid, Guid>
        _streamToClient = new ConcurrentDictionary<Guid, Guid>(); //reverse lookup for _clientToStreams

    //storage for topics and linking with streams
    private readonly IDictionary<string, List<Guid>> _topicToStreams = new ConcurrentDictionary<string, List<Guid>>();
    private readonly IDictionary<Guid, string> _streamToTopic = new ConcurrentDictionary<Guid, string>();


    //storage for subscriptions and linking with clientIds
    private readonly IDictionary<Guid, Tuple<ChannelWriter<T>, CancellationToken>> _subscriptions =
        new ConcurrentDictionary<Guid, Tuple<ChannelWriter<T>, CancellationToken>>();

    private readonly IDictionary<Guid, List<Guid>>
        _clientToSubscriptions = new ConcurrentDictionary<Guid, List<Guid>>();

    private readonly IDictionary<Guid, Guid>
        _subscriptionToClient = new ConcurrentDictionary<Guid, Guid>(); //reverse lookup for _clientToSubscriptions


    //linking topics with subscriptions
    private readonly IDictionary<string, List<Guid>> _topicToSubscription =
        new ConcurrentDictionary<string, List<Guid>>();

    private readonly IDictionary<Guid, string> _subscriptionToTopic = new ConcurrentDictionary<Guid, string>();


    public async Task<Guid> RegisterStream(Guid clientId, ChannelReader<T> stream,
        string topic)
    {
        var streamId = Guid.NewGuid();
        _streams.Add(streamId, stream);
        Console.WriteLine($"Registering stream {streamId} for client {clientId} on topic {topic}");

        if (!_clientToStreams.ContainsKey(clientId))
        {
            _clientToStreams.Add(clientId, new List<Guid>());
        }

        _clientToStreams[clientId].Add(streamId);
        _streamToClient[streamId] = clientId;

        if (!_topicToStreams.ContainsKey(topic))
        {
            _topicToStreams.Add(topic, new List<Guid>());
        }

        _topicToStreams[topic].Add(streamId);
        _streamToTopic[streamId] = topic;

        StreamReader(stream, streamId);

        return streamId;
    }

    /**
     * stop a stream and all subscriptions to it
     * instantly stops all subscriptions
     * tells the client to stop streaming, which will then remove the stream from the client
     */
    public async Task StopStream(Guid streamId)
    {
        Console.WriteLine($"Removing stream {streamId}");
        if (!_streamToClient.ContainsKey(streamId) || !_streams.ContainsKey(streamId))
        {
            Console.WriteLine($"Cannot remove stream {streamId}, it does not exist");
            return;
        }

        //get topic, check if this stream was the only one streaming to it and remove it if so
        var topic = _streamToTopic[streamId];
        var allTopicStreams = _topicToStreams[topic];
        if (allTopicStreams.Count > 1)
        {
            //there are still other streams streaming to this topic, so just remove this stream
            allTopicStreams.Remove(streamId);
            _streamToTopic.Remove(streamId);
        }
        else
        {
            //this is the only stream streaming to this topic, so remove the topic and all subscriptions to it
            _topicToStreams.Remove(topic);
            _streamToTopic.Remove(streamId);

            foreach (var subscriptionId in _topicToSubscription[topic].ToList())
            {
                CloseAndDeleteSubscription(subscriptionId);
            }
        }


        //tell client to cancel streaming
        var senderId = _streamToClient[streamId];
        await (await this._userProxy.Client(senderId)).CancelStream(streamId);

        // do not remove the stream yet, as the client might still be streaming
        // the stream will automatically be removed once the client stops streaming
    }

    /**
     * close a stream absolutely
     * DO NOT call when stream could still send messages
     */
    public Task RemoveStream(Guid streamId)
    {
        return this.CloseStream(streamId);
    }

    public async Task StopTopic(string topic)
    {
        var streams = _topicToStreams[topic];
        foreach (var streamId in streams)
        {
            await StopStream(streamId);
        }
    }

    public async Task<Guid> Subscribe(string topic, Guid clientId, ChannelWriter<T> observer,
        CancellationToken cancellationToken)
    {
        var subscriptionId = Guid.NewGuid();
        Console.WriteLine($"Subscribing {clientId} to {topic} with id {subscriptionId}");
        _subscriptions.Add(subscriptionId, Tuple.Create(observer, cancellationToken));

        if (!_clientToSubscriptions.ContainsKey(clientId))
        {
            _clientToSubscriptions.Add(clientId, new List<Guid>());
        }

        _clientToSubscriptions[clientId].Add(subscriptionId);
        _subscriptionToClient.Add(subscriptionId, clientId);

        if (!_topicToSubscription.ContainsKey(topic))
        {
            _topicToSubscription.Add(topic, new List<Guid>());
        }

        _topicToSubscription[topic].Add(subscriptionId);
        _subscriptionToTopic.Add(subscriptionId, topic);

        //we dont need to do any manual linking between reader and writer, as that is done in the OnStreamValue method

        return subscriptionId;
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        Console.WriteLine($"Unsubscribing {subscriptionId}");
        if (!_subscriptions.ContainsKey(subscriptionId))
        {
            Console.WriteLine($"Cannot unsubscribe {subscriptionId}, it does not exist");
            return;
        }

        CloseAndDeleteSubscription(subscriptionId);
    }

    public void Unsubscribe(string topic, Guid clientId)
    {
        Console.WriteLine($"Unsubscribing {clientId} from {topic}");
        if (!_clientToSubscriptions.ContainsKey(clientId))
        {
            Console.WriteLine($"Cannot unsubscribe {clientId} from Topic {topic}, he is not subscribed");
            return;
        }

        //try getting the subscriptionId given the topic and clientId
        Guid? subscriptionId = null;
        foreach (Guid id in _clientToSubscriptions[clientId].Where(id => _subscriptionToTopic[id] == topic))
        {
            subscriptionId = id;
            break;
        }

        if (subscriptionId == null)
        {
            Console.WriteLine($"Cannot unsubscribe {clientId} from Topic {topic}, he is not subscribed");
            return;
        }

        CloseAndDeleteSubscription(subscriptionId.Value);
    }

    /**
     * asynchronously wait for values from the stream and call OnStreamValue, handle stream faults and completion
     */
    private async Task StreamReader(ChannelReader<T> reader, Guid streamId)
    {
        try
        {
            var val = await reader.WaitToReadAsync();
            while (val)
            {
                while (reader.TryRead(out var value))
                {
                    await OnStreamValue(value, streamId);
                }

                val = await reader.WaitToReadAsync();
            }
        }
        catch (Exception ex)
        {
            await CloseStream(streamId, ex);
            return;
        }

        await CloseStream(streamId);
    }

    /**
     * this method is called when a stream receives a value
     * it will send the value to all subscriptions
     */
    private async Task OnStreamValue(T value, Guid streamId)
    {
        Console.WriteLine($"Stream {streamId} value: {value}");

        async Task SendToReceiver(Guid subscriptionId)
        {
            Console.WriteLine($"Sending value to subscription {subscriptionId}");
            var writer = _subscriptions[subscriptionId].Item1;
            var cancellationToken = _subscriptions[subscriptionId].Item2;
            //check for cancellation and remove subscription
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Detected a cancelled subscription {subscriptionId} in SendValue");
                CloseAndDeleteSubscription(subscriptionId);
                return;
            }

            await writer.WriteAsync(value, cancellationToken);
        }

        if (_streamToTopic.ContainsKey(streamId))
        {
            var topic = _streamToTopic[streamId];
            //write to all subscriptions
            if (_topicToSubscription.ContainsKey(topic))
            {
                Console.WriteLine(
                    $"Valie on topic {topic} will be sent to {string.Join(", ", _topicToSubscription[topic])}");
                var tasks = _topicToSubscription[topic].ToList().Select(SendToReceiver);
                await Task.WhenAll(tasks);
            }
        }
    }

    private void CloseAndDeleteSubscription(Guid subscriptionId, Exception? exception = null)
    {
        Console.WriteLine(
            $"Closing subscription {subscriptionId} on topic {_subscriptionToTopic[subscriptionId]} for client {_subscriptionToClient[subscriptionId]}");
        var writer = _subscriptions[subscriptionId].Item1;
        //if there is an exception complete with exception, else complete normally
        if (exception != null)
        {
            var completed = writer.TryComplete(exception);
            if (!completed)
            {
                Console.WriteLine($"Could not complete subscription {subscriptionId} with exception {exception}");
            }
        }
        else
        {
            var completed = writer.TryComplete();
            if (!completed)
            {
                Console.WriteLine($"Could not complete subscription {subscriptionId}");
            }
        }

        //remove all references to subscription
        _subscriptions.Remove(subscriptionId);
        _clientToSubscriptions[_subscriptionToClient[subscriptionId]].Remove(subscriptionId);
        _subscriptionToClient.Remove(subscriptionId);
        _topicToSubscription[_subscriptionToTopic[subscriptionId]].Remove(subscriptionId);
        _subscriptionToTopic.Remove(subscriptionId);
    }

    private async Task CloseStream(Guid streamId, Exception? exception = null)
    {
        if (exception != null)
        {
            Console.WriteLine($"Stream {streamId} faulted: {exception}");
        }
        else
        {
            Console.WriteLine($"Stream {streamId} completed gracefully");
        }

        //if the stream is still associated with a topic, remove it
        if (_streamToTopic.ContainsKey(streamId))
        {
            //if this is the last stream to a topic, remove the topic
            var topic = _streamToTopic[streamId];
            _streamToTopic.Remove(streamId);
            var allTopicStreams = _topicToStreams[topic];
            if (allTopicStreams.Count > 1)
            {
                Console.WriteLine($"Removing stream {streamId} from topic {topic}");
                allTopicStreams.Remove(streamId);
            }
            else
            {
                Console.WriteLine($"Removing topic {topic}");
                _topicToStreams.Remove(topic);

                foreach (var subscriptionId in _topicToSubscription[topic].ToList())
                {
                    CloseAndDeleteSubscription(subscriptionId, exception);
                }
            }
        }

        //remove association with client
        _clientToStreams.Remove(_streamToClient[streamId]);
        _streamToClient.Remove(streamId);
        _streams.Remove(streamId);
    }
}
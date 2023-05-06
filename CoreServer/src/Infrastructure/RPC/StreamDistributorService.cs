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

    private readonly IDictionary<Guid, List<Guid>> _clientToStream = new ConcurrentDictionary<Guid, List<Guid>>();

    private readonly IDictionary<Guid, Guid>
        _streamToClient = new ConcurrentDictionary<Guid, Guid>(); //reverse lookup for _clientToStream


    //storage for subscriptions and linking with clientIds
    private readonly IDictionary<Guid, Tuple<ChannelWriter<T>, CancellationToken>> _subscriptions =
        new ConcurrentDictionary<Guid, Tuple<ChannelWriter<T>, CancellationToken>>();

    private readonly IDictionary<Guid, List<Guid>> _clientToSubscription = new ConcurrentDictionary<Guid, List<Guid>>();

    private readonly IDictionary<Guid, Guid>
        _subscriptionToClient = new ConcurrentDictionary<Guid, Guid>(); //reverse lookup for _clientToSubscription


    //linking streams with subscriptions
    private readonly IDictionary<Guid, List<Guid>> _streamToSubscription = new ConcurrentDictionary<Guid, List<Guid>>();
    private readonly IDictionary<Guid, Guid> _subscriptionToStream = new ConcurrentDictionary<Guid, Guid>();


    public async Task<Guid> RegisterStream(Guid clientId, ChannelReader<T> stream,
        Guid? streamId = null)
    {
        streamId ??= Guid.NewGuid();
        _streams.Add(streamId.Value, stream);
        if (!_clientToStream.ContainsKey(clientId))
        {
            _clientToStream.Add(clientId, new List<Guid>());
        }

        _clientToStream[clientId].Add(streamId.Value);
        _streamToClient.Add(streamId.Value, clientId);

        StreamReader(stream, streamId.Value);

        return streamId.Value;
    }

    /**
     * stop a stream and all subscriptions to it
     * instantly stops all subscriptions
     * tells the client to stop streaming, which will then remove the stream from the client
     */
    public async Task RemoveStream(Guid streamId)
    {
        if (!_streamToClient.ContainsKey(streamId) || !_streams.ContainsKey(streamId))
        {
            Console.WriteLine($"Cannot remove stream {streamId}, it does not exist");
            return;
        }

        //cancel all subscriptions
        foreach (var subscriptionId in _streamToSubscription[streamId])
        {
            CloseAndDeleteSubscription(subscriptionId);
        }

        //tell client to cancel streaming
        var senderId = _streamToClient[streamId];
        await (await this._userProxy.Client(senderId)).CancelStream(streamId);

        // do not remove the stream yet, as the client might still be streaming
        // the stream will automatically be removed once the client stops streaming
    }

    public async Task<Guid> Subscribe(Guid streamId, Guid clientId, ChannelWriter<T> observer,
        CancellationToken cancellationToken)
    {
        var subscriptionId = Guid.NewGuid();
        _subscriptions.Add(subscriptionId, Tuple.Create(observer, cancellationToken));

        if (!_clientToSubscription.ContainsKey(clientId))
        {
            _clientToSubscription.Add(clientId, new List<Guid>());
        }

        _clientToSubscription[clientId].Add(subscriptionId);
        _subscriptionToClient.Add(subscriptionId, clientId);

        if (!_streamToSubscription.ContainsKey(streamId))
        {
            _streamToSubscription.Add(streamId, new List<Guid>());
        }

        _streamToSubscription[streamId].Add(subscriptionId);
        _subscriptionToStream.Add(subscriptionId, streamId);

        //we dont need to do any manual linking between reader and writer, as that is done in the OnStreamValue method

        return subscriptionId;
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        if (!_subscriptions.ContainsKey(subscriptionId))
        {
            Console.WriteLine($"Cannot unsubscribe {subscriptionId}, it does not exist");
            return;
        }

        CloseAndDeleteSubscription(subscriptionId);
    }

    public void Unsubscribe(Guid streamId, Guid clientId)
    {
        if (!_clientToSubscription.ContainsKey(clientId))
        {
            Console.WriteLine($"Cannot unsubscribe {clientId} from Stream {streamId}, he is not subscribed");
            return;
        }
        Guid? subscriptionId =_clientToSubscription[clientId].Select(g=>(Guid?)g).FirstOrDefault(g=>_subscriptionToStream[g.Value]==streamId);
        if (subscriptionId == null)
        {
            Console.WriteLine($"Cannot unsubscribe {clientId} from Stream {streamId}, he is not subscribed");
            return;
        }
        CloseAndDeleteSubscription(subscriptionId.Value);
    }

    /**
     * asynchroniously wait for values from the stream and call OnStreamValue, handle stream faults and completion
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
            await OnStreamFaulted(streamId, ex);
            return;
        }

        //await OnStreamCompleted(streamId);
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

        //write to all subscriptions
        if (_streamToSubscription.ContainsKey(streamId))
        {
            var tasks = _streamToSubscription[streamId].Select(SendToReceiver);
            await Task.WhenAll(tasks);
        }
    }
    
    private async Task OnStreamCompleted(Guid streamId)
    {
        Console.WriteLine($"Stream {streamId} completed gracefully");
        //close all subscriptions
        _streamToSubscription[streamId].ForEach(subscriptionId => CloseAndDeleteSubscription(subscriptionId));
        //remove stream
        _streams.Remove(streamId);
        _clientToStream[_streamToClient[streamId]].Remove(streamId);
        _streamToClient.Remove(streamId);
        await Task.CompletedTask;
    }

    private void CloseAndDeleteSubscription(Guid subscriptionId, Exception? exception = null)
    {
        var writer = _subscriptions[subscriptionId].Item1;
        //if there is an exception complete with exception, else complete normally
        if (exception != null)
        {
            var completed=writer.TryComplete(exception);
            if (!completed)
            {
                Console.WriteLine($"Could not complete subscription {subscriptionId} with exception {exception}");
            }
        }
        else
        {
            var completed=writer.TryComplete();
            if (!completed)
            {
                Console.WriteLine($"Could not complete subscription {subscriptionId}");
            }
        }

        //remove all references to subscription
        _subscriptions.Remove(subscriptionId);
        _clientToSubscription[_subscriptionToClient[subscriptionId]].Remove(subscriptionId);
        _subscriptionToClient.Remove(subscriptionId);
        _streamToSubscription[_subscriptionToStream[subscriptionId]].Remove(subscriptionId);
        _subscriptionToStream.Remove(subscriptionId);
    }

    private async Task OnStreamFaulted(Guid streamId, Exception exception)
    {
        Console.WriteLine($"Stream {streamId} faulted: {exception}");
        //close all subscriptions
        _streamToSubscription[streamId]
            .ForEach(subscriptionId => CloseAndDeleteSubscription(subscriptionId, exception));
        //remove stream
        _streams.Remove(streamId);
        _clientToStream[_streamToClient[streamId]].Remove(streamId);
        _streamToClient.Remove(streamId);
        await Task.CompletedTask;
    }
}
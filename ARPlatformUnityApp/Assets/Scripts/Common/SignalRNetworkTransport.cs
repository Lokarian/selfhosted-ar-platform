using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Serialization;

public class SignalRNetworkTransport : NetworkTransport
{
    private bool _isServer = false;
    private ulong myClientId = 0;
    private DateTime startTime = DateTime.Now;
    private float Now => (float)(DateTime.Now - startTime).TotalSeconds;

    enum SignalRConnectionState
    {
        Disconnected,
        Connecting,
        Registering,
        SendConnectionRequest,
        Connected
    }


    HubConnection _connection;
    private ulong _serverClientId;
    private NetworkManager _networkManager;
    private SignalRConnectionState _connectionState = SignalRConnectionState.Disconnected;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private Queue<Tuple<ServerMessage, float>> _messageQueue = new();

    //storage for client id to ChannelWriter
    private Dictionary<ulong, Tuple<string, ChannelWriter<ServerMessage>>> _clientWriters = new();

    public void Start()
    {
    }

    public void Update()
    {
        //check if we(the client) are freshly connected and need to send a connection request
        if (_connectionState == SignalRConnectionState.SendConnectionRequest)
        {
            CreateChannelWriter(0, GlobalConfig.Singleton.ArSessionId);
            InvokeOnTransportEvent(NetworkEvent.Connect, myClientId, default, Now);
            _connectionState = SignalRConnectionState.Connected;
        }

        //deque messages
        while (_messageQueue.Count > 0)
        {
            var (serverMessage, receiveTime) = _messageQueue.Dequeue();
            InvokeOnTransportEvent(serverMessage.networkEvent, serverMessage.clientId, serverMessage.payload,
                receiveTime);
        }
    }

    private void StartSignalR()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:5001/api/hub", HttpTransportType.WebSockets,
                options => { options.Headers.Add("Authorization", "Bearer " + GlobalConfig.Singleton.JwtToken); })
            .AddMessagePackProtocol(options =>
            {
                options.SerializerOptions =
                    MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            })
            .Build();
        _connectionState = SignalRConnectionState.Connecting;
        _connection.StartAsync().ContinueWith(async task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log($"There was an error opening the connection: {task.Exception?.GetBaseException()}");
            }
            else
            {
                OnSignalRConnected();
            }
        });
    }

    private async Task OnSignalRConnected()
    {
        Debug.Log("SignalR connection established");
        _connectionState = SignalRConnectionState.Registering;
        var stream = _connection.StreamAsync<ServerMessage>("SubscribeToTopic", GlobalConfig.Singleton.SubscriptionId,
            _cancellationTokenSource.Token);
        _connectionState = SignalRConnectionState.Connected;
        if (!_isServer)
        {
            _connectionState = SignalRConnectionState.SendConnectionRequest;
        }

        await foreach (var message in stream)
        {
            OnServerMessage(message);
        }

        StopSignalRConnection();
    }

    private void StopSignalRConnection()
    {
        _cancellationTokenSource.Cancel();
        _connectionState = SignalRConnectionState.Disconnected;
        Debug.Log($"StopSignalRConnection");
    }

    private void OnServerMessage(ServerMessage serverMessage)
    {
        Debug.Log($"Received SignalR Message: {serverMessage.networkEvent}");
        if (_isServer)
        {
            //check if the client is already connected by checking if we have an outgoing channel writer
            if (!_clientWriters.ContainsKey(serverMessage.clientId))
            {
                //create new channel writer for the client
                CreateChannelWriter(serverMessage.clientId, serverMessage.senderArMemberId);
                //notify the network manager that a new client has connected
                _messageQueue.Enqueue(new Tuple<ServerMessage, float > (new ServerMessage()
                {
                    networkEvent = NetworkEvent.Connect, payload = default, clientId = serverMessage.clientId,
                    senderArMemberId = serverMessage.senderArMemberId
                }, Now));
            }

            //if there is data in the payload, send it to the network manager
            if (serverMessage.payload.Count > 0)
            {
                _messageQueue.Enqueue(new Tuple<ServerMessage, float>(serverMessage, Now));
            }
            else
            {
                Debug.Log($"Received empty payload from client {serverMessage.clientId}");
            }
        }
    }

    public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
    {
        Debug.Log($"Send: clientId={clientId}, payload={payload}, networkDelivery={networkDelivery}");
        if (_connectionState != SignalRConnectionState.Connected)
        {
            return;
        }

        if (!_clientWriters.ContainsKey(clientId))
        {
            Debug.LogWarning($"Could not Send, no writer associated with clientId {clientId}");
            return;
        }

        var writer = _clientWriters[clientId].Item2;
        writer.TryWrite(new ServerMessage()
        {
            clientId = clientId,
            payload = payload,
            senderArMemberId = GlobalConfig.Singleton.SubscriptionId
        });
    }

    private void CreateChannelWriter(ulong clientId, string topic)
    {
        var channel = Channel.CreateUnbounded<ServerMessage>();
        _connection.SendAsync("PublishStream", channel.Reader, topic);
        _clientWriters.Add(clientId, new Tuple<string, ChannelWriter<ServerMessage>>(topic, channel.Writer));
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
    {
        clientId = 0;
        payload = new ArraySegment<byte>();
        receiveTime = 0;
        return NetworkEvent.Nothing;
    }

    public override bool StartClient()
    {
        Debug.Log($"StartClient");
        _isServer = false;
        //parse the first 8 letters of GlobalConfig.Singleton.SubscriptionId as hex to get the clientId
        myClientId = ulong.Parse(GlobalConfig.Singleton.SubscriptionId.Substring(0, 8),
            System.Globalization.NumberStyles.HexNumber);
        StartSignalR();
        return true;
    }

    public override bool StartServer()
    {
        Debug.Log($"StartServer");
        _isServer = true;
        StartSignalR();
        return true;
    }

    public override void DisconnectRemoteClient(ulong clientId)
    {
        Debug.Log($"DisconnectRemoteClient: clientId={clientId}");
    }

    public override void DisconnectLocalClient()
    {
        Debug.Log($"DisconnectLocalClient");
    }

    public override ulong GetCurrentRtt(ulong clientId)
    {
        Debug.Log($"GetCurrentRtt: clientId={clientId}");
        return 10;
    }

    public override void Shutdown()
    {
        Debug.Log($"Shutdown");
        _connection.StopAsync();
    }

    public override void Initialize(NetworkManager networkManager = null)
    {
        _networkManager = networkManager;
        Debug.Log($"Initialize with networkManager={networkManager}");
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    public override ulong ServerClientId => 0;
}

public class ServerMessage
{
    public NetworkEvent networkEvent;
    public ulong clientId;
    public ArraySegment<byte> payload;
    public string senderArMemberId;
}
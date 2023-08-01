using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AOT;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

public class SignalRNetworkTransport2 : NetworkTransport
{
    private bool _isServer = false;
    HubConnection _connection;
    private ConcurrentQueue<ServerMessage> _messageQueue = new();
    private ulong _clientIdCounter = 1;

    private Dictionary<ulong, ChannelWriter<byte[]>> _clientIdToWriter = new();
    private Dictionary<ulong, CancellationTokenSource> _clientIdToStreamCancelToken = new();
    private Dictionary<ulong, string> _clientIdToMemberId = new();


    private ChannelWriter<byte[]> _serverWriter;
    public override ulong ServerClientId => 0;
    public Dictionary<ulong, ulong> ClientIdToRtt = new();

    //[DebugGuiGraph(r:0.27f,g:0.933f,b:0.27f,group:1,max:10000,min:0)]
    public float OutGoingTrafficBytes = 0;
    //[DebugGuiGraph(r:0.27f,g:0.27f,b:0.933f,group:2,max:0,min:-10000)]
    public float IncomingTrafficBytes => -_incomingTrafficBytes;
    public float _incomingTrafficBytes = 0;

    private void LateUpdate()
    {
        OutGoingTrafficBytes=0;
        _incomingTrafficBytes=0;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    public static extern void StartSignalRJs();

    [DllImport("__Internal")]
    public static extern void ProvideDataCallback(MyCallback action);

    [DllImport("__Internal")]
    public static extern void SendByteArrayToSignalR(int data, int length);

    public delegate void MyCallback(IntPtr prt, int length, int eventCode);

    [MonoPInvokeCallback(typeof(MyCallback))]
    public static void Callback(IntPtr prt, int length, int eventCode)
    {
        byte[] managedArray = new byte[length];
        Marshal.Copy(prt, managedArray, 0, length);
        var serverMessage = new ServerMessage()
        {
            networkEvent = (NetworkEvent)eventCode,
            clientId = Singleton.ServerClientId,
            payload = managedArray
        };
        Singleton._messageQueue.Enqueue(serverMessage);
    }

    public static SignalRNetworkTransport2 Singleton;
#endif

    private async Task StartSignalRCs()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{GlobalConfig.Singleton.ServerUrl}/api/unityBrokerHub", HttpTransportType.WebSockets,
                options => { options.Headers.Add("Authorization", "Bearer " + GlobalConfig.Singleton.AccessToken); })
            .AddMessagePackProtocol(options =>
            {
                //the standard signalR resolver serializes enums as string, but we work with int
                options.SerializerOptions =
                    MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            })
            .Build();
        await _connection.StartAsync();
        Debug.Log("SignalR connection started");
    }

    private async Task ServerJoinProcess()
    {
        await _connection.InvokeAsync("RegisterAsServer", GlobalConfig.Singleton.ArSessionId);
        _connection.On("NewClientConnection", async (string memberId) => { await ProcessUserConnection(memberId); });
        _connection.On("ClientDisconnected", async (string memberId) => { await ProcessUserDisconnect(memberId); });
    }

    private async Task ClientJoinProcessCs()
    {
        var myMemberId = await _connection.InvokeAsync<string>("CreateArMember", GlobalConfig.Singleton.ArSessionId,
            (int)ArUserRole.Hololens);
        GlobalConfig.Singleton.MyMemberId = myMemberId;

        //process incoming stream
        var dataStream =
            _connection.StreamAsync<byte[]>("ClientGetOwnStream", myMemberId);
        Task.Run(async () =>
        {
            await foreach (var data in dataStream)
            {
                var serverMessage = new ServerMessage()
                {
                    networkEvent = NetworkEvent.Data,
                    clientId = 0,
                    payload = data
                };
                _messageQueue.Enqueue(serverMessage);
            }
        });

        //register outgoing stream
        var channel = Channel.CreateUnbounded<byte[]>();
        _connection.SendAsync("ClientSendToServer", channel.Reader, GlobalConfig.Singleton.ArSessionId, myMemberId);
        _serverWriter = channel.Writer;

        //wait for server to tell us of bidirectional connection establishment
        _connection.On("ConnectionEstablished", () =>
        {
            Debug.Log("Server notified us of connection establishment");
            _messageQueue.Enqueue(new ServerMessage()
            {
                networkEvent = NetworkEvent.Connect,
                clientId = 0,
                payload = default
            });
        });
        //finally notify of connection establishment
        await _connection.InvokeAsync("NotifyServerOfClient",GlobalConfig.Singleton.ArSessionId, myMemberId);
        Debug.Log("Client joined and notified server");
    }

    private async Task ProcessUserConnection(string memberId)
    {
        Debug.Log($"New client connected: {memberId}");
        var newClientId = _clientIdCounter++;
        _clientIdToMemberId.Add(newClientId, memberId);

        CancellationTokenSource cancelToken = new();
        _clientIdToStreamCancelToken.Add(newClientId, cancelToken);
        //create outgoing stream
        var channel = Channel.CreateUnbounded<byte[]>();
        _connection.SendAsync("ServerSendStreamToMember", channel.Reader, memberId, cancelToken.Token);
        _clientIdToWriter.Add(newClientId, channel.Writer);

        //create incoming stream
        var dataStream = _connection.StreamAsync<byte[]>("ServerGetUserStream", GlobalConfig.Singleton.ArSessionId,
            memberId, cancelToken.Token);
        Task.Run(async () =>
        {
            await foreach (var data in dataStream)
            {
                var serverMessage = new ServerMessage()
                {
                    networkEvent = NetworkEvent.Data,
                    clientId = newClientId,
                    payload = data
                };
                _messageQueue.Enqueue(serverMessage);
            }

            Debug.Log($"ServerGetUserStream ended for {newClientId}, {memberId}");
        });
        _messageQueue.Enqueue(new ServerMessage()
        {
            networkEvent = NetworkEvent.Connect,
            clientId = newClientId,
            payload = default
        });
        //notify of connection establishment
        await _connection.InvokeAsync("NotifyClientOfSuccessfulConnection", memberId);
    }

    private async Task ProcessUserDisconnect(string memberId)
    {
        Debug.Log($"Client disconnected: {memberId}");
        if (!_clientIdToMemberId.ContainsValue(memberId))
        {
            Debug.LogError($"Client disconnected that was not connected: {memberId}");
            return;
        }

        var clientId = _clientIdToMemberId.First(x => x.Value == memberId).Key;
        _clientIdToMemberId.Remove(clientId);
        _clientIdToStreamCancelToken[clientId].Cancel();
        Debug.Log($"Client disconnected and cancellation requested: {clientId}, {memberId}");
    }


    public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
    {
        OutGoingTrafficBytes += payload.Count;
#if UNITY_WEBGL&&!UNITY_EDITOR
        IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(payload.Array, payload.Offset);
        SendByteArrayToSignalR(ptr.ToInt32(), payload.Count);
        return;
#endif
        if (clientId == 0)
        {
            if (_isServer)
                Debug.LogWarning("Server tried to send to clientId 0");
            else
                _serverWriter?.TryWrite(payload.ToArray());
        }
        else
        {
            if (!_isServer)
            {
                Debug.LogWarning("Client tried to send to clientId != 0");
            }
            else
            {
                if (!_clientIdToWriter.ContainsKey(clientId))
                {
                    Debug.LogError($"Server tried to send to clientId {clientId} that is not connected");
                    return;
                }

                _clientIdToWriter[clientId].TryWrite(payload.ToArray());
            }
        }
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
    {
        if (!_messageQueue.TryDequeue(out var serverMessage))
        {
            clientId = 0;
            payload = default;
            receiveTime = 0;
            return NetworkEvent.Nothing;
        }
        _incomingTrafficBytes += serverMessage.payload?.Length ?? 0;
        clientId = serverMessage.clientId;
        payload = serverMessage.payload == null ? default : new ArraySegment<byte>(serverMessage.payload);
        receiveTime = Time.realtimeSinceStartup;
        return serverMessage.networkEvent;
    }

    public override bool StartClient()
    {
        _isServer = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            StartSignalRJs();
#else
        StartSignalRCs().ContinueWith(async (task) =>
        {
            if (task.IsFaulted)
                Debug.LogError(task.Exception);
            else
                await ClientJoinProcessCs();
        });
#endif
        return true;
    }

    public override bool StartServer()
    {
        _isServer = true;
        StartSignalRCs().ContinueWith(async (task) =>
        {
           if (task.IsFaulted)
               Debug.LogError(task.Exception);
           else
               await ServerJoinProcess(); 
        });
        return true;
    }

    public override void DisconnectRemoteClient(ulong clientId)
    {
        if (_clientIdToStreamCancelToken.TryGetValue(clientId, out var cancelToken))
            cancelToken.Cancel();
    }

    public override void DisconnectLocalClient()
    {
        _messageQueue.Enqueue(new ServerMessage()
        {
            networkEvent = NetworkEvent.Disconnect,
            clientId = 0,
            payload = default
        });
    }

    public override ulong GetCurrentRtt(ulong clientId)
    {
        var rtt = ClientIdToRtt.ContainsKey(clientId) ? ClientIdToRtt[clientId] : 0;
        return rtt;
    }

    public override void Shutdown()
    {
        Debug.Log($"Shutdown");
        _connection?.StopAsync();
    }

    public override void Initialize(NetworkManager networkManager = null)
    {
#if UNITY_WEBGL &&!UNITY_EDITOR
            ProvideDataCallback(Callback);
            Singleton = this;
#endif
    }
}


public class ServerMessage
{
    public NetworkEvent networkEvent;
    public ulong clientId;
    public byte[] payload;
}

public enum ArUserRole
{
    Hololens,
    Web
}
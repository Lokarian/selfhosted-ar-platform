using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using NetworkTransport = Unity.Netcode.NetworkTransport;

public class SignalRNetworkTransport : NetworkTransport
{
    private bool _isServer = false;

    HubConnection _connection;
    private readonly ulong _serverClientId = 0;

    private Queue<ServerMessage> _messageQueue = new();

    private ulong _clientIdCounter = 1;

    //storage for client id to ChannelWriter
    private Dictionary<ulong, string> _clientIdToMemberId = new();
    private Dictionary<string, ulong> _memberIdToClientId = new();
    private Dictionary<string, ChannelWriter<ServerMessage>> _memberIdToWriter = new();
    private ChannelWriter<ServerMessage> _serverWriter;
    public Dictionary<ulong, ulong> ClientIdToRtt = new();


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
            senderArMemberId = "",
            clientId = Singleton._serverClientId,
            payload = managedArray
        };
        Singleton._messageQueue.Enqueue(serverMessage);
    }

    public static SignalRNetworkTransport Singleton;
#endif

    private async Task StartSignalR()
    {
#if UNITY_WEBGL&&!UNITY_EDITOR
        Debug.Log("Starting SignalR from c# to js");
        StartSignalRJs();
#else
        _connection = new HubConnectionBuilder()
            .WithUrl($"{GlobalConfig.Singleton.ServerUrl}/api/hub", HttpTransportType.WebSockets,
                options => { options.Headers.Add("Authorization", "Bearer " + GlobalConfig.Singleton.AccessToken); })
            .AddMessagePackProtocol(options =>
            {
                StaticCompositeResolver.Instance.Register(
            MessagePack.Resolvers.GeneratedResolver.Instance,
            MessagePack.Resolvers.StandardResolver.Instance
        );
                options.SerializerOptions = MessagePackSerializerOptions.Standard
                    .WithResolver(StaticCompositeResolver.Instance)
                    .WithSecurity(MessagePackSecurity.UntrustedData);
            })
            .Build();
        await _connection.StartAsync();
        Debug.Log("SignalR connection established");
        await JoinArSession();
        if (_isServer)
        {
            var metaStream =
                _connection.StreamAsync<StreamMetaEvent>("SubscribeToTopic",
                    $"{GlobalConfig.Singleton.ArSessionId}/meta");
            Task.Run(async () =>
            {
                await foreach (var metaEvent in metaStream)
                {
                    Debug.Log($"Received meta event: {metaEvent}");
                    OnMetaEvent(metaEvent);
                }
            });
            var dataStream =
                _connection.StreamAsync<ServerMessage>("SubscribeToTopic", $"{GlobalConfig.Singleton.ArSessionId}");
            Task.Run(async () =>
            {
                await foreach (var serverMessage in dataStream)
                {
                    //Debug.Log($"Received SignalR Message: {serverMessage.networkEvent}");
                    OnServerMessage(serverMessage);
                }
            });
        }
        else
        {
            var channel = Channel.CreateUnbounded<ServerMessage>();
            _connection.SendAsync("PublishStreamWithContextUserId", channel.Reader, GlobalConfig.Singleton.ArSessionId,
                GlobalConfig.Singleton.MyMemberId);
            _serverWriter = channel.Writer;
            var dataStream =
                _connection.StreamAsync<ServerMessage>("SubscribeToTopic", GlobalConfig.Singleton.MyMemberId);
            Task.Run(async () =>
            {
                await foreach (var serverMessage in dataStream)
                {
                    //Debug.Log($"Received SignalR Message: {serverMessage.networkEvent}");
                    OnServerMessage(serverMessage);
                }
            });
        }
#endif
    }

    private async Task JoinArSession()
    {
        var connectionId = await _connection.InvokeAsync<Guid>("InitializeConnection", new List<string>());
        if (_isServer)
        {
            GlobalConfig.Singleton.MyMemberId = connectionId.ToString();
            return;
        }

        var joinRequest = new JoinArSessionCommand()
        {
            arSessionId = GlobalConfig.Singleton.ArSessionId,
            role = GlobalConfig.Singleton.MyBuildTarget switch
            {
                ArBuildTarget.Hololens => ArUserRole.Hololens,
                ArBuildTarget.Web => ArUserRole.Web,
                _ => throw new ArgumentOutOfRangeException()
            }
        };
        var json = JsonUtility.ToJson(joinRequest);
        using UnityWebRequest webRequest =
            new UnityWebRequest(GlobalConfig.Singleton.ServerUrl + "/api/Ar/JoinArSession", "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("Authorization", "Bearer " + GlobalConfig.Singleton.AccessToken);
        webRequest.SetRequestHeader("userconnectionid", connectionId.ToString());
        var operation = webRequest.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Yield();
        }

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to join AR session");
            throw new Exception("Failed to join AR session");
        }

        ArMemberDto arMemberDto = JsonUtility.FromJson<ArMemberDto>(webRequest.downloadHandler.text);
        GlobalConfig.Singleton.MyMemberId = arMemberDto.id;
        Debug.Log($"Joined AR session as {arMemberDto.role}");
    }

    private void OnMetaEvent(StreamMetaEvent metaEvent)
    {
        switch (metaEvent.Type)
        {
            case StreamMetaEventType.PublisherDisconnected:
                if (_memberIdToClientId.ContainsKey(metaEvent.ClientId.ToString()))
                {
                    _messageQueue.Enqueue(new ServerMessage()
                    {
                        networkEvent = (int)NetworkEvent.Disconnect,
                        clientId = _memberIdToClientId[metaEvent.ClientId.ToString()],
                        senderArMemberId = metaEvent.ClientId.ToString(),
                        payload = default
                    });
                    //remove from dictionaries
                    _clientIdToMemberId.Remove(_memberIdToClientId[metaEvent.ClientId.ToString()]);
                    _memberIdToClientId.Remove(metaEvent.ClientId.ToString());
                    _memberIdToWriter.Remove(metaEvent.ClientId.ToString());
                }

                break;
        }
    }


    private void OnServerMessage(ServerMessage serverMessage)
    {
        if (_isServer)
        {
            if (!_memberIdToClientId.TryGetValue(serverMessage.senderArMemberId, out var clientId))
            {
                clientId = _clientIdCounter++;
                _memberIdToClientId[serverMessage.senderArMemberId] = clientId;
                _clientIdToMemberId[clientId] = serverMessage.senderArMemberId;
                // create channel 
                var channel = Channel.CreateUnbounded<ServerMessage>();
                _connection.SendAsync("PublishStream", channel.Reader, serverMessage.senderArMemberId);
                _memberIdToWriter[serverMessage.senderArMemberId] = channel.Writer;
                _messageQueue.Enqueue(new ServerMessage()
                {
                    networkEvent = (int)NetworkEvent.Connect,
                    clientId = clientId,
                    senderArMemberId = serverMessage.senderArMemberId,
                    payload = default
                });
            }

            serverMessage.clientId = clientId;
            _messageQueue.Enqueue(serverMessage);
        }
        else
        {
            _messageQueue.Enqueue(serverMessage);
        }
    }

    public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
    {
#if UNITY_WEBGL&&!UNITY_EDITOR
        IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(payload.Array, payload.Offset);
        SendByteArrayToSignalR(ptr.ToInt32(), payload.Count);
#else
        //Debug.Log("Sending: "+payload.Count+" bytes to "+clientId);
        if (!_isServer)
        {
            if (!_serverWriter.TryWrite(new ServerMessage()
            {
                networkEvent = (int)NetworkEvent.Data,
                clientId = 0,
                payload = payload.ToArray(),
                senderArMemberId = GlobalConfig.Singleton.MyMemberId
            }))
            {
                Debug.Log($"Send: failed to write to channel for clientId={clientId}");
            }

            return;
        }

        if (!_clientIdToMemberId.TryGetValue(clientId, out var memberId))
        {
            Debug.Log($"Send: clientId={clientId} not found in _clientIdToMemberId");
            return;
        }

        if (!_memberIdToWriter.TryGetValue(memberId, out var writer))
        {
            Debug.Log($"Send: memberId={memberId} not found in _memberIdToWriter");
            return;
        }

        if (!writer.TryWrite(new ServerMessage()
        {
            networkEvent = (int)NetworkEvent.Data,
            clientId = _serverClientId,
            payload = payload.ToArray(),
            senderArMemberId = GlobalConfig.Singleton.MyMemberId
        }))
        {
            Debug.Log($"Send: failed to write to channel for clientId={clientId}");
        }
#endif
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
    {
        if (_messageQueue.Count > 0)
        {
            var message = _messageQueue.Dequeue();
            clientId = message.clientId;
            payload = message.payload;
            receiveTime = Time.realtimeSinceStartup;
            //Debug.Log($"Roll Event: {(message.payload?.Length ?? 0)} bytes from {message.clientId}");
            return (NetworkEvent)message.networkEvent;
        }

        clientId = 0;
        payload = default;
        receiveTime = 0;
        return NetworkEvent.Nothing;
    }

    public override bool StartClient()
    {
        Debug.Log($"StartClient");
        _isServer = false;
        StartSignalR().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log($"StartClient: failed to start SignalR: {task.Exception}");
            }
            else
            {
                _messageQueue.Enqueue(new ServerMessage()
                {
                    networkEvent = (int)NetworkEvent.Connect,
                    clientId = 0,
                    senderArMemberId = GlobalConfig.Singleton.MyMemberId,
                    payload = default
                });
            }
        });
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
        var rtt = ClientIdToRtt.ContainsKey(clientId) ? ClientIdToRtt[clientId] : 0;
        Debug.Log("Rtt: " + rtt + "ms");
        return rtt;
    }

    public override void Shutdown()
    {
        Debug.Log($"Shutdown");
        _connection?.StopAsync();
    }

    public override void Initialize(NetworkManager networkManager = null)
    {
        Debug.Log($"Initialize with networkManager={networkManager}");
#if UNITY_WEBGL &&!UNITY_EDITOR
        ProvideDataCallback(Callback);
        Singleton = this;
#endif
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    public override ulong ServerClientId => 0;
}

[MessagePackObject]
public class ServerMessage
{
    [Key(0)]
    public int networkEvent;
    [Key(1)]
    public ulong clientId;
    [Key(2)]
    public byte[] payload;
    [Key(3)]
    public string senderArMemberId;
}



public enum StreamMetaEventType
{
    SubscriberConnected,
    SubscriberDisconnected,
    PublisherConnected,
    PublisherDisconnected,
}

public struct StreamMetaEvent
{
    public StreamMetaEventType Type;
    public string Topic;
    public Guid ClientId;
}

[Serializable]
public class JoinArSessionCommand
{
    public string arSessionId;
    public ArUserRole role;
}

public enum ArUserRole
{
    Hololens,
    Web
}

[Serializable]
public class ArMemberDto
{
    public string id;
    public string baseMemberId;
    public string userId;
    public string sessionId;
    public DateTime? DeletedAt;

    public ArUserRole role;
}
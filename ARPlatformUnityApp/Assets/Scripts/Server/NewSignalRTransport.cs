using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Unity.Netcode;
using UnityEngine;

public class NewSignalRTransport : NetworkTransport
{
    private bool _isServer = false;
    private ulong myClientId = 0;
    private DateTime startTime = DateTime.Now;
    private float Now => (float)(DateTime.Now - startTime).TotalSeconds;
    
    private ulong _clientIdCounter = 1;
    
    HubConnection _connection;
    
    private Queue<Tuple<ServerMessage, float>> _messageQueue = new();
    
    private Dictionary<ulong, string> clientIdToUserId = new();
    private Dictionary<string,ulong> userIdToClientId = new();

    private async Task StartSignalR()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:5001/api/hub", HttpTransportType.WebSockets,
                options => { options.Headers.Add("Authorization", "Bearer " + GlobalConfig.Singleton.AccessToken); })
            .AddMessagePackProtocol(options =>
            {
                options.SerializerOptions =
                    MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            })
            .Build();
        _connection.On("ReceiveMessage", (ServerMessage message) => OnSignalRMessage(message));
        await _connection.StartAsync();
    }

    private void OnSignalRMessage(ServerMessage message)
    {
        Debug.Log("Received signalR message");
        if(!userIdToClientId.TryGetValue(message.senderArMemberId,out var clientId))
        {
            clientId = _clientIdCounter++;
            userIdToClientId.Add(message.senderArMemberId,clientId);
            clientIdToUserId.Add(clientId,message.senderArMemberId);
            Debug.Log("New client on Transport, assigning id " + clientId);
            _messageQueue.Enqueue(new Tuple<ServerMessage, float>(new ServerMessage()
            {
                networkEvent = NetworkEvent.Connect,
                clientId = clientId,
                payload = default
            }, Now));
        }
        //change clientId to the one we assigned
        message.clientId = clientId;
        _messageQueue.Enqueue(new Tuple<ServerMessage, float>(message, Now));
    }
    public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
    {
        Debug.Log("Sending signalR message");
        if (_isServer)
        {
            if(clientIdToUserId.TryGetValue(clientId,out var userId))
            {
                _connection.SendAsync("SendMessageToClient", userId, new ServerMessage()
                {
                    payload = payload.ToArray(),
                    senderArMemberId = "2a6f442a-1b8d-4ff0-8014-a5fed4b21db7",
                    clientId = 0,//todo check if this is correct
                    networkEvent = NetworkEvent.Data
                });
            }
        }
        else
        {
            _connection.SendAsync("SendMessageToClient","2a6f442a-1b8d-4ff0-8014-a5fed4b21db7", new ServerMessage()
            {
                payload = payload.ToArray(),
                senderArMemberId = "6087dc1b-dafa-4e26-882d-cc734559868a",
                clientId = 0,//todo check if this is correct
                networkEvent = NetworkEvent.Data
            });
        }
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
    {
        if (_messageQueue.Count > 0)
        {
            var message = _messageQueue.Dequeue();
            clientId = message.Item1.clientId;
            payload = message.Item1.payload;
            receiveTime = message.Item2;
            return message.Item1.networkEvent;
        }
        else
        {
            clientId = 0;
            payload = default;
            receiveTime = 0;
            return NetworkEvent.Nothing;
        }
    }

    public override bool StartClient()
    {
        _isServer = false;
        StartSignalR().ContinueWith(task =>
        {
            _messageQueue.Enqueue(new Tuple<ServerMessage, float>(new ServerMessage()
            {
                payload = default,
                senderArMemberId = "6087dc1b-dafa-4e26-882d-cc734559868a",
                clientId = 0,
                networkEvent = NetworkEvent.Connect
            }, Now));
        });
        return true;
    }

    public override bool StartServer()
    {
        _isServer = true;
        StartSignalR();
        return true;
    }
    

    public override void DisconnectRemoteClient(ulong clientId)
    {
        
    }

    public override void DisconnectLocalClient()
    {
        
    }

    public override ulong GetCurrentRtt(ulong clientId)
    {
        throw new NotImplementedException();
    }

    public override void Shutdown()
    {
        
    }

    public override void Initialize(NetworkManager networkManager = null)
    {
        
    }

    public override ulong ServerClientId { get; }
}

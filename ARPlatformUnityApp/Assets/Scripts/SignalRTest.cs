using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.AspNetCore.SignalR.Client;

public class SignalRTest : MonoBehaviour
{
    HubConnection _connection;

    // Start is called before the first frame update
    void Start()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7270/messageHub")
            .Build();
        _connection.StartAsync();
        _connection.On<string, string>("ReceiveMessage",
            (user, message) => { Debug.Log($"Received message from {user}: {message}"); });
        _connection.On<string>("Echo", (message) => Debug.Log(message));
    }

    // Update is called once per frame
    void Update()
    {
    }

    //add this method as a button to unity ui
    public void SendTestMessage()
    {
        _connection.InvokeAsync("SendMessage", "Unity", "Test von Unity");
    }
}
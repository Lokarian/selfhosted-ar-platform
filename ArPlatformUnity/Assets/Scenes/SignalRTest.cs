using System.Collections;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

public class SignalRTest : MonoBehaviour
{
    // Start is called before the first frame update
    public void Init()
    {
        HubConnection connection = new HubConnectionBuilder()
            .WithUrl("https://reithmeir.duckdns.org:5001/api/hub", HttpTransportType.WebSockets,
                options =>
                {
                    options.Headers.Add("Authorization",
                        "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjdlOWY1NzdhLTM2MDMtNDFiZi1iNjYxLWMxMmI0MWNhMTRiYyIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.wAVRRavTj-bjkPRU9JRxqGK8nIWS76OYvWeufKwA3ZI");
                }).AddMessagePackProtocol(options =>
            {
                //the standard signalR resolver serializes enums as string, but we work with int
                options.SerializerOptions =
                    MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            })
            .Build();
        connection.StartAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log("There was an error opening the connection:" + task.Exception.GetBaseException());
                return;
            }
            
            Debug.Log("Connected");
            connection.On("Echo", (string message) => { Debug.Log("Got message on echo: " + message); });
            connection.InvokeAsync("Echo", "Hello World");
            connection.On("EchoBytes", (byte[] message) => { Debug.Log("Got message on echo bytes: " + message.Length); });
            connection.InvokeAsync("EchoBytes", new byte[] {1, 2, 3, 4, 5});
            connection.InvokeAsync<string>("InitializeConnection", new List<string>(){"Unity"}).ContinueWith(task1 =>
            {
                if (task1.IsFaulted)
                {
                    Debug.Log("There was an error opening the connection:" + task1.Exception.GetBaseException());
                    return;
                }
                Debug.Log("Got message on InitializeConnection: " + task1.Result);
            });
            
        });
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 100, 20), "Connect"))
        {
            Init();
        }
    }
}

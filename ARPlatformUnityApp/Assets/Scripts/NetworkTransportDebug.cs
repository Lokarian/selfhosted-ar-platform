using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkTransportDebug : MonoBehaviour
{
    public NetworkTransport transport;
    // Start is called before the first frame update
    void Start()
    {
        transport.OnTransportEvent += Transport_OnTransportEvent;
    }

    private void Transport_OnTransportEvent(NetworkEvent eventtype, ulong clientid, ArraySegment<byte> payload, float receivetime)
    {
        Debug.Log($"TransportEvent: {eventtype} {clientid} {payload} {receivetime}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

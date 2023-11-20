using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BandwidthTester : NetworkBehaviour
{
    public NetworkVariable<int> BytesPerTimeFrameUpstream = new NetworkVariable<int>(10000);
    public NetworkVariable<int> BytesPerTimeFrameDownstream = new NetworkVariable<int>(10000);
    public NetworkVariable<float> Timeframe = new NetworkVariable<float>(0.05f);
    
    private float _lastTimeframeBegin;
    private void Update()
    {
        if (Time.realtimeSinceStartup - _lastTimeframeBegin >= Timeframe.Value)
        {
            _lastTimeframeBegin = Time.realtimeSinceStartup;
            //loop through all clients and add to DebugGUI graph
            if (IsServer)
            {
                var bytes = new byte[BytesPerTimeFrameDownstream.Value];
                ReceiveBytes_ClientRpc(bytes);
            }
            else
            {
                var bytes = new byte[BytesPerTimeFrameUpstream.Value];
                ReceiveBytes_ServerRpc(bytes);
            }
        }
    }
    [ClientRpc]
    private void ReceiveBytes_ClientRpc(byte[] bytes)
    {
        
    }
    [ServerRpc(RequireOwnership = false)]
    private void ReceiveBytes_ServerRpc(byte[] bytes, ServerRpcParams serverRpcParams = default)
    {
    }
}

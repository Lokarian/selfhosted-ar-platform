using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class BandwidthAllocator : NetworkBehaviour
{
    public NetworkVariable<int> BytesPerTimeFrame = new NetworkVariable<int>(30000);
    public NetworkVariable<float> Timeframe = new NetworkVariable<float>(0.05f);
    private Dictionary<ulong, int> _clientBytesUsed = new Dictionary<ulong, int>();
    public static BandwidthAllocator Singleton;
    private float _lastTimeframeBegin;

    [DebugGUIGraph()]
    public float Traffic = 0.0f;
    private void Start()
    {
        _lastTimeframeBegin = Time.realtimeSinceStartup;
    }

    private void LateUpdate()
    {
        if (Time.realtimeSinceStartup - _lastTimeframeBegin >= Timeframe.Value)
        {
            _lastTimeframeBegin = Time.realtimeSinceStartup;
            //loop through all clients and add to DebugGUI graph
            if(_clientBytesUsed.Keys.Count> 0)
            {
                Traffic = _clientBytesUsed.Values.Max();
            }
            else
            {
                Traffic = 0;
            }
            _clientBytesUsed.Clear();
        }
    }

    public int GetUsedBytesForClient(ulong clientId)
    {
        if (_clientBytesUsed.ContainsKey(clientId))
        {
            return _clientBytesUsed[clientId];
        }

        return 0;
    }

    private void AllocateBytesToClient(ulong clientId, int bytes)
    {
        if (_clientBytesUsed.ContainsKey(clientId))
        {
            _clientBytesUsed[clientId] += bytes;
        }
        else
        {
            _clientBytesUsed.Add(clientId, bytes);
        }
    }

    public bool CanAllocateBytesToClient(ulong clientId, int bytes)
    {
        if (_clientBytesUsed.ContainsKey(clientId))
        {
            return _clientBytesUsed[clientId] + bytes <= BytesPerTimeFrame.Value;
        }

        return bytes <= BytesPerTimeFrame.Value;
    }

    public bool TryAllocateFixedBytesToClient(ulong clientId, int bytes)
    {
        if (CanAllocateBytesToClient(clientId, bytes))
        {
            AllocateBytesToClient(clientId, bytes);
            return true;
        }

        return false;
    }
    /**
     * Tries to allocate bytes to a client, but will only allocate as many bytes as the client can receive in the current frame.
     * Returns true if any bytes were allocated.
     */
    public bool TryAllocateBytesToClient(ulong clientId, int bytes, out int bytesAllocated)
    {
        var allocatableBytes = BytesPerTimeFrame.Value - GetUsedBytesForClient(clientId);
        if (allocatableBytes > 0)
        {
            bytesAllocated = Math.Min(allocatableBytes, bytes);
            AllocateBytesToClient(clientId, bytesAllocated);
            return true;
        }
        bytesAllocated = 0;
        return false;
    }

    private void OnEnable()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Debug.LogError("There can only be one BandwidthAllocator in the scene");
        }
    }
}
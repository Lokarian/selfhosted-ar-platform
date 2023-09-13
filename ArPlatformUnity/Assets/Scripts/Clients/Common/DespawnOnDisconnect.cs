using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DespawnOnDisconnect : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }
    
    private void OnClientDisconnect(ulong clientId)
    {
    }
}

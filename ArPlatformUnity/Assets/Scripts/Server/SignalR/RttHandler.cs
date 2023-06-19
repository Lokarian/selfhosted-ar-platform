using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RttHandler : NetworkBehaviour
{
    public ulong rttIntervalMs = 1000;
    public SignalRNetworkTransport2 SignalRNetworkTransport;

    private static Dictionary<ulong, ulong> _clientIdToRtt = new();
    private static Dictionary<ulong, ClientRpcParams> _clientIdToClientRpcParams = new();
    

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        if (!SignalRNetworkTransport)
        {
            SignalRNetworkTransport = NetworkManager.Singleton.gameObject.GetComponent<SignalRNetworkTransport2>();
        }

        if (IsServer)
        {
            StartCoroutine(RttLoop());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RespondToRttRequest_ServerRpc(float timeSinceStartup, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        var rtt = (ulong)((Time.realtimeSinceStartup - timeSinceStartup) * 1000);
        //update the rtt for this client
        _clientIdToRtt[clientId] = rtt;
        SignalRNetworkTransport.ClientIdToRtt[clientId] = rtt;
    }

    [ClientRpc]
    public void RttRequest_ClientRpc(ulong currentRtt, float timeSinceStartup,
        ClientRpcParams clientRpcParams = default)
    {
        RespondToRttRequest_ServerRpc(timeSinceStartup);
        if (SignalRNetworkTransport?.ClientIdToRtt != null)
        {
            SignalRNetworkTransport.ClientIdToRtt[SignalRNetworkTransport.ServerClientId] = currentRtt;
        }
    }

    IEnumerator RttLoop()
    {
        while (true)
        {
            //send rtt request to all clients
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                //if we don't have a client rpc params for this client, create one
                if (!_clientIdToClientRpcParams.TryGetValue(clientId, out var clientRpcParams))
                {
                    clientRpcParams = new ClientRpcParams
                        { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } };
                    _clientIdToClientRpcParams[clientId] = clientRpcParams;
                }

                if (!_clientIdToRtt.TryGetValue(clientId, out var currentRtt))
                {
                    currentRtt = 0;
                }

                RttRequest_ClientRpc(currentRtt, Time.realtimeSinceStartup, clientRpcParams);
            }

            yield return new WaitForSecondsRealtime(rttIntervalMs / 1000f);
        }
    }
}
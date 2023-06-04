using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RTTHandler : NetworkBehaviour
{
    private static Dictionary<ulong, ulong> _clientIdToRtt = new();
    private static Dictionary<ulong, ClientRpcParams> _clientIdToClientRpcParams = new();

    private ulong _rttCounter = 0;

    //store start time of a rtt request with a unique id with a capactiy of 10
    private ulong lastRttRequestTime = 0;

    public ulong rttIntervalMs = 1000;

    private ulong Now => (ulong)(Time.realtimeSinceStartup * 1000);

    public SignalRNetworkTransport SignalRNetworkTransport;

    // Start is called before the first frame update
    void Start()
    {
        SignalRNetworkTransport = NetworkManager.Singleton.gameObject.GetComponent<SignalRNetworkTransport>();
        if (IsServer)
        {
            StartCoroutine(RttLoop());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RespondToRttRequest_ServerRpc(ulong rttRequestCounter, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        var rtt = Now - lastRttRequestTime;
        //if the request took longer than the interval, add the interval to the rtt
        if (rttRequestCounter != _rttCounter - 1)
        {
            rtt += rttIntervalMs * (rttRequestCounter - _rttCounter + 1);
        }

        //update the rtt for this client
        _clientIdToRtt[clientId] = rtt;
        SignalRNetworkTransport.ClientIdToRtt[clientId] = rtt;
    }

    [ClientRpc]
    public void RttRequest_ClientRpc(ulong currentRtt, ulong rttRequestCounter,
        ClientRpcParams clientRpcParams = default)
    {
        RespondToRttRequest_ServerRpc(rttRequestCounter);
        SignalRNetworkTransport.ClientIdToRtt[SignalRNetworkTransport.ServerClientId] = currentRtt;
    }

    IEnumerator RttLoop()
    {
        while (true)
        {
            lastRttRequestTime = Now;
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

                RttRequest_ClientRpc(currentRtt, _rttCounter++, clientRpcParams);
            }

            yield return new WaitForSecondsRealtime(rttIntervalMs / 1000f);
        }
    }
}
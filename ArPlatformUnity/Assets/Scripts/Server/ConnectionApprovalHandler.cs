using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ConnectionApprovalHandler : MonoBehaviour
{
        public bool currentlyLocked = false;
        
        //public prefab for the player
        public GameObject hololensPlayerPrefab;
        public GameObject webXrPlayerPrefab;
    // Start is called before the first frame update
    void Start()
    {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (currentlyLocked)
        {
            response.Approved = false;
            response.Reason = "locked";
            return;
        }
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;

        // Additional connection data defined by user code
        var connectionData = request.Payload;

        // Your approval logic determines the following values
        response.Approved = true;
        Debug.Log("Connection Data: " + System.Text.Encoding.ASCII.GetString(connectionData));
        response.CreatePlayerObject = true;
        if (System.Text.Encoding.ASCII.GetString(connectionData)=="Hololens")
        {
            response.CreatePlayerObject = true;
            response.PlayerPrefabHash=hololensPlayerPrefab.GetComponent<NetworkObject>().PrefabIdHash;
        }
        else if (System.Text.Encoding.ASCII.GetString(connectionData)=="WebXR")
        {
            response.CreatePlayerObject = true;
            // The Prefab hash value of the NetworkPrefab, if null the default NetworkManager player Prefab is used
            response.PlayerPrefabHash = webXrPlayerPrefab.GetComponent<NetworkObject>().PrefabIdHash;
        }

        // Position to spawn the player object (if null it uses default of Vector3.zero)
        response.Position = Vector3.zero;

        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
        response.Rotation = Quaternion.identity;
    
        // If response.Approved is false, you can provide a message that explains the reason why via ConnectionApprovalResponse.Reason
        // On the client-side, NetworkManager.DisconnectReason will be populated with this message via DisconnectReasonMessage
        //response.Reason = "Some reason for not approving the client";

        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
        
        Debug.Log("Approving connection request from " + clientId);
    }

}

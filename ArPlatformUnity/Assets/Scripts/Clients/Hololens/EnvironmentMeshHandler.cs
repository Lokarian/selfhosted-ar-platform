using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;

public class EnvironmentMeshHandler : NetworkBehaviour
{
    public static bool allowStart = false;

    public ARMeshManager arMeshManager;
    public GameObject NetworkMeshPrefab;
    private List<string> _pendingUpdates = new();
    private IDictionary<string, GameObject> _spatialMeshNameToNetworkMesh = new Dictionary<string, GameObject>();


    public void Start()
    {
        base.OnNetworkSpawn();
        if ((NetworkManager.Singleton.LocalClient?.PlayerObject?.CompareTag("HololensPlayer") ?? false) && allowStart)
        {
            arMeshManager = GameObject.Find("ARSpatialMeshManager").GetComponent<ARMeshManager>();
            RequestOwnership_ServerRpc();
        }
    }

    public void StartTrackingEnvironment()
    {
        if (NetworkManager.Singleton.LocalClient?.PlayerObject?.CompareTag("HololensPlayer") ?? false)
        {
            RequestOwnership_ServerRpc();
        }
        else
        {
            Debug.Log("Not registering EnvironmentMeshHandler");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestOwnership_ServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            if (client.PlayerObject?.CompareTag("HololensPlayer") ?? false)
            {
                GetComponent<NetworkObject>().ChangeOwnership(clientId);
                Debug.Log("Gave ownership to HololensPlayer for EnvironmentMeshHandler");
            }
        }
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (IsServer)
        {
            return;
        }

        var existingMeshes = arMeshManager.meshes;

        foreach (var mesh in existingMeshes)
        {
            RequestNetworkMesh_ServerRpc($"EnvironmentNetworkMesh_{mesh.name}");
            AddPendingUpdate(mesh.name);
        }

        arMeshManager.meshesChanged += ArMeshManagerOnMeshesChanged;
    }

    private void ArMeshManagerOnMeshesChanged(ARMeshesChangedEventArgs eventData)
    {
        eventData.added.ForEach(x =>
        {
            RequestNetworkMesh_ServerRpc($"EnvironmentNetworkMesh_{x.name}");
            AddPendingUpdate(x.name);
        });
        eventData.updated.ForEach(x =>
        {
            if (!_spatialMeshNameToNetworkMesh.TryGetValue(x.name, out var networkMesh))
            {
                RequestNetworkMesh_ServerRpc($"EnvironmentNetworkMesh_{x.name}");
                AddPendingUpdate(x.name);
            }
            else
            {
                //check if mesh ist still alive
                if (!networkMesh)
                {
                    return;
                }

                //remove all pending updates for this mesh
                _pendingUpdates.RemoveAll(y => y == x.name);
                //apply the update
                networkMesh.GetComponent<MeshFilter>().sharedMesh = x.mesh;
                networkMesh.transform.position = x.transform.position;
            }
        });
    }

    [ServerRpc]
    void RequestNetworkMesh_ServerRpc(string gameObjectName, ServerRpcParams rpcParams = default)
    {
        if (GameObject.Find(gameObjectName))
        {
            Debug.Log($"Mesh already exists: {gameObjectName}");
            return;
        }

        GameObject go = Instantiate(NetworkMeshPrefab, Vector3.zero, Quaternion.identity);
        go.name = gameObjectName;
        go.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId);
        go.GetComponent<NetworkObject>().DontDestroyWithOwner = true;
        var senderParams = new ClientRpcParams()
        {
            Send = new ClientRpcSendParams()
            {
                TargetClientIds = new[] { rpcParams.Receive.SenderClientId }
            }
        };
        go.GetComponent<NetworkMesh>().RemoveMeshRenderer_ClientRpc(senderParams);
        NotifyMeshSpawned_ClientRpc(gameObjectName, go.GetComponent<NetworkObject>().NetworkObjectId, senderParams);
    }

    //clientRpc to notify environmentMeshHandler that mesh was spawned
    [ClientRpc]
    void NotifyMeshSpawned_ClientRpc(string gameObjectName, ulong meshNetworkObjectId,
        ClientRpcParams rpcParams = default)
    {
        GameObject go = NetworkManager.SpawnManager.SpawnedObjects[meshNetworkObjectId].gameObject;
        if (!go)
        {
            return;
        }

        var realMeshName = gameObjectName.Replace("EnvironmentNetworkMesh_", "");
        var pendingUpdate = _pendingUpdates.FirstOrDefault(x => x == realMeshName);
        if (pendingUpdate != null)
        {
            var spatialMeshGameObject = GameObject.Find(realMeshName)?.gameObject;
            if (!spatialMeshGameObject)
            {
                Debug.LogWarning($"Mesh not found for pending update {realMeshName}");
                return;
            }

            _spatialMeshNameToNetworkMesh.Add(spatialMeshGameObject.name, go);
            go.GetComponent<MeshFilter>().sharedMesh = spatialMeshGameObject.GetComponent<MeshFilter>().sharedMesh;
            go.transform.position = spatialMeshGameObject.transform.position;
            _pendingUpdates.Remove(pendingUpdate);
        }
    }

    void AddPendingUpdate(string meshName)
    {
        _pendingUpdates.RemoveAll(x => x == meshName);
        _pendingUpdates.Add(meshName);
    }
}
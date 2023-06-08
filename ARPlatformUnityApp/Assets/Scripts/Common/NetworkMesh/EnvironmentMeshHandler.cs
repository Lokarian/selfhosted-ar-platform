using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Unity.Netcode;
using UnityEngine;
using SpatialAwarenessHandler =
    Microsoft.MixedReality.Toolkit.SpatialAwareness.IMixedRealitySpatialAwarenessObservationHandler<
        Microsoft.MixedReality.Toolkit.SpatialAwareness.SpatialAwarenessMeshObject>;


public class EnvironmentMeshHandler : NetworkBehaviour, SpatialAwarenessHandler
{
    protected bool isRegistered = false;
    public GameObject NetworkMeshPrefab;
    private List<string> _pendingUpdates = new();
    private IDictionary<string, GameObject> _spatialMeshNameToNetworkMesh = new Dictionary<string, GameObject>();

    protected virtual void Start()
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

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (IsServer)
        {
            return;
        }

        var existingMeshes = CoreServices.SpatialAwarenessSystem.SpatialAwarenessObjectParent
            .GetComponentsInChildren<MeshFilter>()
            .Select(x => x.gameObject).ToList();
        foreach (var mesh in existingMeshes)
        {
            RequestNetworkMesh_ServerRpc($"EnvironmentNetworkMesh_{mesh.name}");
            AddPendingUpdate(mesh.name);
        }

        RegisterEventHandlers<SpatialAwarenessHandler, SpatialAwarenessMeshObject>();
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
            }
        }
    }

    void AddPendingUpdate(string meshName)
    {
        _pendingUpdates.RemoveAll(x => x == meshName);
        _pendingUpdates.Add(meshName);
    }


    protected virtual void OnDisable()
    {
        UnregisterEventHandlers<SpatialAwarenessHandler, SpatialAwarenessMeshObject>();
    }

    protected virtual void OnDestroy()
    {
        UnregisterEventHandlers<SpatialAwarenessHandler, SpatialAwarenessMeshObject>();
    }

    protected virtual void RegisterEventHandlers<T, U>()
        where T : IMixedRealitySpatialAwarenessObservationHandler<U>
        where U : BaseSpatialAwarenessObject
    {
        if (!isRegistered && (CoreServices.SpatialAwarenessSystem != null))
        {
            CoreServices.SpatialAwarenessSystem.RegisterHandler<T>(this);
            isRegistered = true;
            Debug.Log("Registered EnvironmentMeshHandler");
        }
    }

    protected virtual void UnregisterEventHandlers<T, U>()
        where T : IMixedRealitySpatialAwarenessObservationHandler<U>
        where U : BaseSpatialAwarenessObject
    {
        if (isRegistered && (CoreServices.SpatialAwarenessSystem != null))
        {
            CoreServices.SpatialAwarenessSystem.UnregisterHandler<T>(this);
            isRegistered = false;
        }
    }

    /// <inheritdoc />
    public virtual void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        RequestNetworkMesh_ServerRpc($"EnvironmentNetworkMesh_{eventData.SpatialObject.GameObject.name}");
        AddPendingUpdate(eventData.SpatialObject.GameObject.name);
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
        go.GetComponent<NetworkObject>().DontDestroyWithOwner=true;
        go.GetComponent<NetworkMesh>().RemoveMeshRenderer_ClientRpc(new ClientRpcParams()
        {
            Send = new ClientRpcSendParams()
            {
                TargetClientIds = new[] { rpcParams.Receive.SenderClientId }
            }
        });
        NotifyMeshSpawned_ClientRpc(gameObjectName, go.GetComponent<NetworkObject>().NetworkObjectId,
            new ClientRpcParams()
            {
                Send = new ClientRpcSendParams()
                {
                    TargetClientIds = new[] { rpcParams.Receive.SenderClientId }
                }
            });
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
            _pendingUpdates.Remove(pendingUpdate);
        }
    }

    /// <inheritdoc />
    public virtual void OnObservationUpdated(
        MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        if (!_spatialMeshNameToNetworkMesh.TryGetValue(eventData.SpatialObject.GameObject.name, out var networkMesh))
        {
            RequestNetworkMesh_ServerRpc($"EnvironmentNetworkMesh_{eventData.SpatialObject.GameObject.name}");
            AddPendingUpdate(eventData.SpatialObject.GameObject.name);
        }
        else
        {
            //check if mesh ist still alive
            if (!networkMesh)
            {
                return;
            }

            //remove all pending updates for this mesh
            _pendingUpdates.RemoveAll(x => x == eventData.SpatialObject.GameObject.name);
            //apply the update
            networkMesh.GetComponent<MeshFilter>().sharedMesh = eventData.SpatialObject.Filter.mesh;
        }
    }

    /// <inheritdoc />
    public virtual void OnObservationRemoved(
        MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        //do nothing
    }
}
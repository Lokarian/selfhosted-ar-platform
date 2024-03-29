using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;

public class EnvironmentMeshHandler : NetworkBehaviour
{
    public static EnvironmentMeshHandler Singleton;
    
    public ARMeshManager arMeshManager;
    public GameObject NetworkMeshPrefab;
    private readonly Queue<MeshFilter> _updatesToBePerformed = new();
    
    private readonly IDictionary<string, NetworkMesh> _spatialMeshNameToNetworkMesh =
        new Dictionary<string, NetworkMesh>();

    private readonly IDictionary<string, int> _spatialMeshNameToUniqueMeshId = new Dictionary<string, int>();
    private readonly IDictionary<int, string> _uniqueMeshIdToSpatialMeshName = new Dictionary<int, string>();
    private int _uniqueMeshIdCounter = 0;

    public NetworkVariable<float> MinTimeBetweenAllUpdates = new(3);
    public NetworkVariable<float> MinTimeBetweenUpdatesPerMesh = new(10f);
    public NetworkVariable<bool> AllowUpdates = new(true);
    private readonly Dictionary<MeshFilter, float> _lastUpdated = new Dictionary<MeshFilter, float>();
    private readonly List<MeshFilter> _pendingUpdates = new List<MeshFilter>();


    public Dictionary<int, List<Vector3>> _verticesChunks = new Dictionary<int, List<Vector3>>();
    public Dictionary<int, List<int>> _indicesChunks = new Dictionary<int, List<int>>();

    private void Start()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (GlobalConfig.Singleton.ShowEnvironment)
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

        foreach (var meshFilter in arMeshManager.meshes)
        {
            AddUpdateToBePerformed(meshFilter);
        }

        arMeshManager.meshesChanged += ArMeshManagerOnMeshesChanged;
        StartCoroutine(SendMeshCoroutine());
        StartCoroutine(DequeuePendingUpdatesCoroutine());
    }
    
    public IEnumerator DequeuePendingUpdatesCoroutine()
    {
        while (true)
        {
            yield return null;
            if(!AllowUpdates.Value)
            {
                continue;
            }
            if(_updatesToBePerformed.Count>0)
            {
                continue;
            }
            var lastUpdate = _lastUpdated.Values.Count > 0 ? _lastUpdated.Values.Max() : 0;
            if (lastUpdate + MinTimeBetweenAllUpdates.Value > Time.realtimeSinceStartup)
            {
                continue;
            }
            var meshFilter = _pendingUpdates.FirstOrDefault(x => !_lastUpdated.ContainsKey(x) || _lastUpdated[x] + MinTimeBetweenUpdatesPerMesh.Value <
                Time.realtimeSinceStartup);
            if (meshFilter == null)
            {
                continue;
            }
            _pendingUpdates.Remove(meshFilter);
            AddUpdateToBePerformed(meshFilter);
        }
    }
    

    private void ArMeshManagerOnMeshesChanged(ARMeshesChangedEventArgs eventData)
    {
        eventData.added.ForEach(x => { _pendingUpdates.Add(x);} );
        eventData.updated.ForEach(x => { _pendingUpdates.Add(x);} );
        eventData.removed.ForEach(x => { RemoveMesh_ServerRpc(x.gameObject.name); });
    }

    public IEnumerator SendMeshCoroutine()
    {
        while (true)
        {
            if (_updatesToBePerformed.TryDequeue(out var meshFilter))
            {
                var meshId = _spatialMeshNameToUniqueMeshId[meshFilter.gameObject.name];
                if (meshId == -1)
                {
                    //not yet ready to send mesh, try again later
                    AddUpdateToBePerformed(meshFilter);
                    yield return null;
                    continue;
                }

                var mesh = meshFilter.mesh;
                var verticesLeft = mesh.vertices.ToList();
                var trianglesLeft = mesh.triangles.ToList();
                var bytesLeft = verticesLeft.Count * 12 + 4 * trianglesLeft.Count;
                var chunkNumber = 0;
                while (bytesLeft > 0)
                {
                    //check if we can allocate bytes to client, prevent overflow of send buffer
                    if (BandwidthAllocator.Singleton.TryAllocateBytesToClient(0, bytesLeft, out var actualBytes))
                    {
                        var verticesToSend = verticesLeft.Take(actualBytes / 12).ToArray();
                        verticesLeft.RemoveRange(0, verticesToSend.Length);

                        var bytesLeftForTriangles = actualBytes - verticesToSend.Length * 12;
                        var trianglesToSend = trianglesLeft.Take(bytesLeftForTriangles / 4).ToArray();
                        trianglesLeft.RemoveRange(0, trianglesToSend.Length);
                        bytesLeft = verticesLeft.Count * 12 + 4 * trianglesLeft.Count;

                        UpdateMeshChunk_ServerRpc(meshId, verticesToSend, trianglesToSend, chunkNumber, bytesLeft == 0);

                        chunkNumber++;
                    }

                    yield return null;
                }
                _lastUpdated[meshFilter] = Time.realtimeSinceStartup;
            }

            yield return null;
        }
    }
    
    [ServerRpc]
    void UpdateMeshChunk_ServerRpc(int meshId, Vector3[] vertices, int[] triangles, int chunkNumber, bool lastChunk)
    {
        if (chunkNumber == 0)
        {
            if (_verticesChunks.ContainsKey(meshId))
            {
                if (_verticesChunks[meshId].Count > 0)
                {
                    Debug.LogError(
                        $"Received first chunk of mesh {_uniqueMeshIdToSpatialMeshName[meshId]}, but previous chunk was not yet processed");
                    _verticesChunks[meshId].Clear();
                    _indicesChunks[meshId].Clear();
                }
            }
            else
            {
                _verticesChunks.Add(meshId, new List<Vector3>());
                _indicesChunks.Add(meshId, new List<int>());
            }
        }

        _verticesChunks[meshId].AddRange(vertices);
        _indicesChunks[meshId].AddRange(triangles);
        if (lastChunk)
        {
            var networkMesh = _spatialMeshNameToNetworkMesh[_uniqueMeshIdToSpatialMeshName[meshId]];
            if (_verticesChunks.Count > 0)
                MeshProcessor.Singleton.EnqueueMesh(networkMesh, _verticesChunks[meshId].ToArray(),
                    _indicesChunks[meshId].ToArray());
            _verticesChunks[meshId].Clear();
            _indicesChunks[meshId].Clear();
        }
    }

    public NetworkMesh CreateNetworkMesh(string gameObjectName, ulong clientId)
    {
        GameObject go = Instantiate(NetworkMeshPrefab, Vector3.zero, Quaternion.identity);

        go.name = gameObjectName;
        go.GetComponent<NetworkObject>().CheckObjectVisibility = (id) => id != clientId;
        go.GetComponent<NetworkObject>().Spawn();
        go.transform.parent = transform;
        //go.GetComponent<NetworkObject>().NetworkHide(clientId);
        return go.GetComponent<NetworkMesh>();
    }


    [ServerRpc]
    public void RemoveMesh_ServerRpc(FixedString128Bytes meshName)
    {
        if (!_spatialMeshNameToNetworkMesh.TryGetValue(meshName.ToString(), out var networkMesh))
        {
            return;
        }

        Destroy(networkMesh);
        MeshProcessor.Singleton.RemoveMesh(gameObject.name);
    }

    
    private void AddUpdateToBePerformed(MeshFilter meshFilter)
    {
        var gameObject = meshFilter.gameObject;
        if (!gameObject)
        {
            return;
        }

        //check if we already have a unique id for this mesh, if not create one and set it to -1
        if (!_spatialMeshNameToUniqueMeshId.TryGetValue(gameObject.name, out _))
        {
            _spatialMeshNameToUniqueMeshId.Add(gameObject.name, -1);
            RequestUniqueIdForMesh_ServerRpc(gameObject.name, gameObject.transform.position);
        }

        if (!_updatesToBePerformed.Contains(meshFilter))
        {
            _updatesToBePerformed.Enqueue(meshFilter);
        }
    }


    [ServerRpc]
    public void RequestUniqueIdForMesh_ServerRpc(string name, Vector3 position, ServerRpcParams rpcParams = default)
    {
        var networkMesh = CreateNetworkMesh(name, rpcParams.Receive.SenderClientId);
        networkMesh.transform.position = position;
        _spatialMeshNameToNetworkMesh.Add(name, networkMesh);
        var id = _uniqueMeshIdCounter++;
        _spatialMeshNameToUniqueMeshId[name] = id;
        _uniqueMeshIdToSpatialMeshName[id] = name;
        SetUniqueIdForMesh_ClientRpc(name, id);
    }

    [ClientRpc]
    public void SetUniqueIdForMesh_ClientRpc(string name, int uniqueId)
    {
        _spatialMeshNameToUniqueMeshId[name] = uniqueId;
    }
}
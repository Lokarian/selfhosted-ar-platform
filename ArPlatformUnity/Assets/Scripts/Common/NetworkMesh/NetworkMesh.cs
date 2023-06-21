using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkMesh : NetworkBehaviour
{
    private Mesh previousMesh;
    private List<Vector3> _verticesChunks = new();
    private List<int> _indicesChunks = new();
    private List<Vector2> _uvChunks = new();
    private Dictionary<ulong, Coroutine> _sendMeshCoroutines = new();


    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn");
        base.OnNetworkSpawn();
        //request initial mesh from server if we are not the server and not the owner
        if (!IsServer && !IsOwner)
        {
            Debug.Log("Requesting initial mesh from server");
            RequestInitialMesh_ServerRpc();
        }
    }

    // Update is called once per frame
    void Update()
    {
        //only owner or server can send mesh
        if (!(IsOwner || IsServer))
        {
            return;
        }

        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;
        if (mesh != previousMesh)
        {
            previousMesh = mesh;
            if (mesh != null)
            {
                foreach (var sendMeshCoroutine in _sendMeshCoroutines)
                {
                    if (sendMeshCoroutine.Value != null)
                    {
                        StopCoroutine(sendMeshCoroutine.Value);
                    }
                }
                _sendMeshCoroutines.Clear();

                if (IsServer)
                {
                    //send to all clients except owner
                    foreach (var networkClient in NetworkManager.Singleton.ConnectedClientsList)
                    {
                        if (networkClient.ClientId != OwnerClientId)
                        {
                            _sendMeshCoroutines.Add(networkClient.ClientId,
                                StartCoroutine(SendMeshCoroutine(mesh, networkClient.ClientId)));
                        }
                    }
                }
                else
                {
                    _sendMeshCoroutines.Add(0, StartCoroutine(SendMeshCoroutine(mesh, 0)));
                }
            }
        }
    }

    public IEnumerator SendMeshCoroutine(Mesh mesh, ulong clientId)
    {
        
        var verticesLeft = mesh.vertices.ToList();
        var trianglesLeft = mesh.triangles.ToList();
        var uvsLeft = mesh.uv.ToList();
        var bytesLeft = verticesLeft.Count * 12 + 4 * trianglesLeft.Count+8*uvsLeft.Count;
        
        var chunkNumber = 0;
        while (bytesLeft > 0)
        {
            //check if we can allocate bytes to client, prevent overflow of send buffer
            if (BandwidthAllocator.Singleton.TryAllocateBytesToClient(clientId, bytesLeft, out var actualBytes))
            {
                var verticesToSend = verticesLeft.Take(actualBytes / 12).ToArray();
                verticesLeft.RemoveRange(0, verticesToSend.Length);

                actualBytes -= verticesToSend.Length * 12;
                var trianglesToSend = trianglesLeft.Take(actualBytes / 4).ToArray();
                trianglesLeft.RemoveRange(0, trianglesToSend.Length);
                actualBytes -= trianglesToSend.Length * 4;
                var uvsToSend = uvsLeft.Take(actualBytes / 8).ToArray();
                uvsLeft.RemoveRange(0, uvsToSend.Length);
                
                bytesLeft -= verticesToSend.Length * 12 + trianglesToSend.Length * 4+uvsToSend.Length*8;

                
                if (clientId == 0)
                {
                    UpdateMeshChunk_ServerRpc(verticesToSend, trianglesToSend,uvsToSend, chunkNumber, bytesLeft == 0);
                }
                else
                {
                    UpdateMeshChunk_ClientRpc(verticesToSend, trianglesToSend,uvsToSend, chunkNumber, bytesLeft == 0,
                        new ClientRpcParams()
                            { Send = new ClientRpcSendParams() { TargetClientIds = new[] { clientId } } });
                }

                chunkNumber++;
            }

            yield return null;
        }
        _sendMeshCoroutines.Remove(clientId);
    }

    [ClientRpc]
    public void RemoveMeshRenderer_ClientRpc(ClientRpcParams clientRpcParams = default)
    {
        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer)
        {
            Destroy(meshRenderer);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestInitialMesh_ServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;
        if (mesh != null && !_sendMeshCoroutines.ContainsKey(serverRpcParams.Receive.SenderClientId))
        {
            _sendMeshCoroutines.Add(serverRpcParams.Receive.SenderClientId,
                StartCoroutine(SendMeshCoroutine(mesh, serverRpcParams.Receive.SenderClientId)));
        }
    }


    [ServerRpc]
    void UpdateMeshChunk_ServerRpc(Vector3[] vertices, int[] triangles,Vector2[] uvs, int chunkNumber, bool lastChunk,
        ServerRpcParams serverRpcParams = default)
    {
        UpdateMeshChunk(vertices, triangles,uvs, chunkNumber, lastChunk);
    }

    [ClientRpc]
    void UpdateMeshChunk_ClientRpc(Vector3[] vertices, int[] triangles,Vector2[] uvs, int chunkNumber, bool lastChunk,
        ClientRpcParams clientRpcParams = default)
    {
        UpdateMeshChunk(vertices, triangles,uvs, chunkNumber, lastChunk);
    }

    void UpdateMeshChunk(Vector3[] vertices, int[] triangles,Vector2[] uvs, int chunkNumber, bool lastChunk)
    {
        if (chunkNumber == 0)
        {
            _verticesChunks.Clear();
            _indicesChunks.Clear();
            _uvChunks.Clear();
        }

        _verticesChunks.AddRange(vertices);
        _indicesChunks.AddRange(triangles);
        _uvChunks.AddRange(uvs);
        if (lastChunk)
        {
            var mesh = new Mesh
            {
                vertices = _verticesChunks.ToArray(),
                triangles = _indicesChunks.ToArray()
            };
            if (_uvChunks.Count > 0)
            {
                mesh.uv = _uvChunks.ToArray();
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            SetMesh(mesh);
        }
    }


    public bool GetMeshRepresentation(out Vector3[] verices, out int[] triangles)
    {
        verices = null;
        triangles = null;
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;
        if (mesh != null)
        {
            verices = mesh.vertices;
            triangles = mesh.triangles;
            return true;
        }

        return false;
    }

    public void SetMesh(Mesh mesh)
    {
        var meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }
    
}
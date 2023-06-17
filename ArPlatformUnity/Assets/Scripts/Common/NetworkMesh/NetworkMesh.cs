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
    private List<Coroutine> _sendMeshCoroutines = new();


    [DebugGUIGraph(group: 1)] public float TotalBytesGenerated = 0;

    [DebugGUIGraph(group: 1)] public float TotalBytesSent = 0;


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
                TotalBytesGenerated += mesh.triangles.Length * 12 + 4 * mesh.vertices.Length;

                _sendMeshCoroutines.ForEach(coroutine =>
                {
                    if (coroutine != null) StopCoroutine(coroutine);
                });
                
                if (IsServer)
                {
                    //send to all clients except owner
                    foreach (var networkClient in NetworkManager.Singleton.ConnectedClientsList)
                    {
                        if (networkClient.ClientId != OwnerClientId)
                        {
                            _sendMeshCoroutines.Add(StartCoroutine(SendMeshCoroutine(mesh, networkClient.ClientId)));
                        }
                    }
                }
                else
                {
                    _sendMeshCoroutines.Add(StartCoroutine(SendMeshCoroutine(mesh, 0)));
                }
            }
        }
    }

    public IEnumerator SendMeshCoroutine(Mesh mesh, ulong clientId)
    {
        //Debug.Log($"{mesh.triangles.Length*12+4*mesh.vertices.Length} with {mesh.triangles.Length} {mesh.vertices.Length}");
        var verticesLeft = mesh.vertices.ToList();
        var trianglesLeft = mesh.triangles.ToList();
        var bytesLeft = verticesLeft.Count * 12 + 4 * trianglesLeft.Count;
        var chunkNumber = 0;
        while (bytesLeft > 0)
        {
            //check if we can allocate bytes to client, prevent overflow of send buffer
            if (BandwidthAllocator.Singleton.TryAllocateBytesToClient(clientId, bytesLeft, out var actualBytes))
            {
                var verticesToSend = verticesLeft.Take(actualBytes / 12).ToArray();
                verticesLeft.RemoveRange(0, verticesToSend.Length);

                var bytesLeftForTriangles = actualBytes - verticesToSend.Length * 12;
                var trianglesToSend = trianglesLeft.Take(bytesLeftForTriangles / 4).ToArray();
                trianglesLeft.RemoveRange(0, trianglesToSend.Length);
                bytesLeft = verticesLeft.Count * 12 + 4 * trianglesLeft.Count;
                if (clientId == 0)
                {
                    Debug.Log(
                        $"Sending bytes: {verticesToSend.Length * 12 + 4 * trianglesToSend.Length} at Frame: {Time.frameCount}");
                    TotalBytesSent += verticesToSend.Length * 12 + 4 * trianglesToSend.Length;
                    UpdateMeshChunk_ServerRpc(verticesToSend, trianglesToSend, chunkNumber, bytesLeft == 0);
                }
                else
                {
                    UpdateMeshChunk_ClientRpc(verticesToSend, trianglesToSend, chunkNumber, bytesLeft == 0,
                        new ClientRpcParams()
                            { Send = new ClientRpcSendParams() { TargetClientIds = new[] { clientId } } });
                }

                chunkNumber++;
            }

            yield return null;
        }
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
        if (mesh != null)
        {
            TotalBytesGenerated += mesh.triangles.Length * 12 + 4 * mesh.vertices.Length;
            _sendMeshCoroutines.Add(StartCoroutine(SendMeshCoroutine(mesh, serverRpcParams.Receive.SenderClientId)));
        }
    }


    [ServerRpc]
    void UpdateMeshChunk_ServerRpc(Vector3[] vertices, int[] triangles, int chunkNumber, bool lastChunk,
        ServerRpcParams serverRpcParams = default)
    {
        UpdateMeshChunk(vertices, triangles, chunkNumber, lastChunk);
        var allClientIds = NetworkManager.Singleton.ConnectedClientsList.Select(x => x.ClientId);
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                //send to all clients except the owner
                TargetClientIds = allClientIds.Except(new[] { OwnerClientId }).ToList()
            }
        };
    }

    [ClientRpc]
    void UpdateMeshChunk_ClientRpc(Vector3[] vertices, int[] triangles, int chunkNumber, bool lastChunk,
        ClientRpcParams clientRpcParams = default)
    {
        UpdateMeshChunk(vertices, triangles, chunkNumber, lastChunk);
    }

    void UpdateMeshChunk(Vector3[] vertices, int[] triangles, int chunkNumber, bool lastChunk)
    {
        if (chunkNumber == 0)
        {
            _verticesChunks.Clear();
            _indicesChunks.Clear();
        }

        _verticesChunks.AddRange(vertices);
        _indicesChunks.AddRange(triangles);
        if (lastChunk)
        {
            var mesh = new Mesh();
            mesh.SetVertices(_verticesChunks);
            mesh.SetTriangles(_indicesChunks, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            if (mesh != null)
            {
                GetComponent<MeshFilter>().mesh = mesh;
            }
        }
    }
}
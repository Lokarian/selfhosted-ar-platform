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
    private Coroutine _sendMeshCoroutine;


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner && !IsServer)
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
                if (_sendMeshCoroutine != null)
                {
                    StopCoroutine(_sendMeshCoroutine);
                }

                _sendMeshCoroutine = StartCoroutine(SendMeshCoroutine(mesh));
            }
        }
    }

    public IEnumerator SendMeshCoroutine(Mesh mesh)
    {
        var maxSendSize = 1000;
        //max from vertices and triangles
        var chunkCount = Math.Max(1,
            Math.Max(Math.Ceiling(mesh.vertices.Length / (float)maxSendSize),
                Math.Ceiling(mesh.triangles.Length / (float)maxSendSize)));
        for (var i = 0; i < chunkCount; i++)
        {
            var vertices = mesh.vertices.Skip(i * maxSendSize).Take(maxSendSize).ToArray();
            var triangles = mesh.triangles.Skip(i * maxSendSize).Take(maxSendSize).ToArray();
            var lastChunk = i == chunkCount - 1;
            UpdateMeshChunk_ServerRpc(vertices, triangles, i, lastChunk);
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
        UpdateMeshChunk_ClientRpc(vertices, triangles, chunkNumber, lastChunk, clientRpcParams);
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

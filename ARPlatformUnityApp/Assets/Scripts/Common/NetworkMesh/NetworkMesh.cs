using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkMesh : NetworkBehaviour
{
    private enum NetworkMeshBehavior
    {
        Copying,//the refenced mesh lies on another object and due to mrtk and netcode for gameobjects, we need to copy the mesh
        Self//the mesh lies on this object
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("NetworkMesh spawned"+MeshName.Value);
    }

    private MeshFilter _meshFilter;
    private Mesh previousMesh;
    private List<byte[]> meshDataChunks = new List<byte[]>();
    public NetworkVariable<FixedString128Bytes> MeshName = new();
    private GameObject meshObjectToCopy;
    private NetworkMeshBehavior behavior=NetworkMeshBehavior.Self;

    // Start is called before the first frame update
    void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        previousMesh = _meshFilter.sharedMesh;
        if(MeshName.Value.Length>0)
        {
            gameObject.name = MeshName.Value.ToString();
        }
        MeshName.OnValueChanged += (previousValue, newValue) =>
        {
            if (newValue.Length > 0)
            {
                gameObject.name = newValue.ToString();
            }
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
        {
            return;
        }
        
        if (behavior==NetworkMeshBehavior.Copying&&_meshFilter.sharedMesh != previousMesh)
        {
            previousMesh = _meshFilter.sharedMesh;
            SendMeshToServer();
        }
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (IsClient)
        {
            behavior = NetworkMeshBehavior.Copying;
        }
    }
    
    public void SetReferenceMesh(GameObject meshObject)
    {
        meshObjectToCopy = meshObject;
        _meshFilter = meshObject.GetComponent<MeshFilter>();
        transform.position = meshObject.transform.position;
        transform.rotation = meshObject.transform.rotation;
        transform.localScale = meshObject.transform.localScale;
        SendMeshToServer();
    }
    
    void SendMeshToServer()
    {
        if(!_meshFilter.sharedMesh)
        {
            return;
        }
        Debug.Log("Sending mesh to server");
        byte[] meshBytes = MeshSerializer.SerializeMesh(_meshFilter.sharedMesh);
        var chunkSize = 10000;
        var chunkCount = meshBytes.Length / chunkSize;
        if (meshBytes.Length % chunkSize != 0)
        {
            chunkCount++;
        }

        for (var i = 0; i < chunkCount; i++)
        {
            var chunk = meshBytes.Skip(i * chunkSize).Take(chunkSize).ToArray();
            var lastChunk = i == chunkCount - 1;
            UpdateMeshChunk_ServerRpc(chunk, i, lastChunk);
        }
    }

    [ServerRpc]
    void UpdateMeshChunk_ServerRpc(byte[] meshBytes, int chunkNumber, bool lastChunk)
    {
        UpdateMeshChunk(meshBytes, chunkNumber, lastChunk);
        var allClientIds = NetworkManager.Singleton.ConnectedClientsList.Select(x => x.ClientId);
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                //send to all clients except the owner
                TargetClientIds = allClientIds.Except(new[] { OwnerClientId }).ToList()
            }
        };
        UpdateMeshChunk_ClientRpc(meshBytes, chunkNumber, lastChunk);
    }

    [ClientRpc]
    void UpdateMeshChunk_ClientRpc(byte[] meshBytes, int chunkNumber, bool lastChunk,
        ClientRpcParams clientRpcParams = default)
    {
        UpdateMeshChunk(meshBytes, chunkNumber, lastChunk);
    }

    void UpdateMeshChunk(byte[] meshBytes, int chunkNumber, bool lastChunk)
    {
        if (chunkNumber == 0)
        {
            meshDataChunks.Clear();
        }

        meshDataChunks.Add(meshBytes);
        if (lastChunk)
        {
            var totalLength = meshDataChunks.Sum(chunk => chunk.Length);
            var byteArray = new byte[totalLength];
            var offset = 0;
            foreach (var chunk in meshDataChunks)
            {
                Array.Copy(chunk, 0, byteArray, offset, chunk.Length);
                offset += chunk.Length;
            }

            var mesh = MeshSerializer.DeserializeMesh(byteArray);
            if (mesh != null)
            {
                _meshFilter.mesh = mesh;
            }
        }
    }
}
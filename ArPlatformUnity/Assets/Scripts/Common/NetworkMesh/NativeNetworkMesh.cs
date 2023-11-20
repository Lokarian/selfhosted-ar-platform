using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NativeNetworkMesh : NetworkBehaviour
{
    private Mesh previousMesh;

    public NetworkVariable<VerticesContainer> vertices = new NetworkVariable<VerticesContainer>(new(),
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
    //public NetworkVariable<NativeList<Vector3>> vertices = new NetworkVariable<NativeList<Vector3>>{Value = new NativeList<Vector3>(Allocator.Persistent)};
    /*public NetworkVariable<List<int>> triangles = new NetworkVariable<List<int>>(new List<int>(),
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        */

    private bool sameFrame = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //vertices.OnValueChanged += OnVerticesChanged;
        //triangles.OnValueChanged += OnTrianglesChanged;
    }

    private void OnVerticesChanged(VerticesContainer previousvalue, VerticesContainer newvalue)
    {
         Debug.Log($"OnVerticesChanged {newvalue.myArr.Length}");
    }

    private void OnTrianglesChanged(List<int> previousvalue, List<int> newvalue)
    {
        Debug.Log($"OnTrianglesChanged {newvalue.Count}");
    }


    private void Update()
    {
        if (sameFrame)
        {
            Debug.Log("Frame After");
            sameFrame = false;
        }

        if (!IsOwner)
        {
            return;
        }

        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;
        if (mesh != null && mesh != previousMesh)
        {
            previousMesh = mesh;
            Debug.Log($"Update {mesh.vertices.Length}");
            
            vertices.Value.myArr = mesh.vertices;
            //triangles.Value = mesh.triangles.ToList();
            
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
}
public class VerticesContainer : INetworkSerializable
{
    public Vector3[] myArr;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            if (myArr==null)
            {
                return;
            }
            Debug.Log(myArr.ToString());
            serializer.GetFastBufferWriter().WriteValueSafe(myArr);
        }
        else
        {
            serializer.GetFastBufferReader().ReadValueSafe(out myArr);
        }
    }
}
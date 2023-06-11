using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
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
        var meshFilter = GetComponent<MeshFilter>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner && !IsServer)
        {
            return;
        }

        /*
        if (_meshSerializationJobHandle.HasValue && _meshSerializationJobHandle.Value.IsCompleted &&
            _meshBytes.HasValue)
        {
            var meshBytes = _meshBytes.Value.ToArray();
            var chunkSize = 10000;
            var chunkCount = meshBytes.Length / chunkSize;
            if (meshBytes.Length % chunkSize != 0)
            {
                chunkCount++;
            }

            if (IsServer)
            {
                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        //send to all clients except the owner
                        TargetClientIds = NetworkManager.Singleton.ConnectedClientsList.Select(x => x.ClientId)
                            .Except(new[] { OwnerClientId }).ToList()
                    }
                };
                for (var i = 0; i < chunkCount; i++)
                {
                    var chunk = meshBytes.Skip(i * chunkSize).Take(chunkSize).ToArray();
                    var lastChunk = i == chunkCount - 1;
                    UpdateMeshChunk_ClientRpc(chunk, i, lastChunk, clientRpcParams);
                }
            }
            else
            {
                for (var i = 0; i < chunkCount; i++)
                {
                    var chunk = meshBytes.Skip(i * chunkSize).Take(chunkSize).ToArray();
                    var lastChunk = i == chunkCount - 1;
                    UpdateMeshChunk_ServerRpc(chunk, i, lastChunk);
                }
            }
        }
        */

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


#region License and information

/* * * * *
 * A quick mesh serializer that allows to serialize a Mesh as byte array. It should
 * support any kind of mesh including skinned meshes, multiple submeshes, different
 * mesh topologies as well as blendshapes. I tried my best to avoid unnecessary data
 * by only serializing information that is present. It supports Vector4 UVs. The index
 * data may be stored as bytes, ushorts or ints depending on the actual highest used
 * vertex index within a submesh. It uses a tagging system for optional "chunks". The
 * base information only includes the vertex position array and the submesh count.
 * Everything else is handled through optional chunks.
 * 
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2018 Markus Göbel (Bunny83)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * * * * */

#endregion License and information

[System.Serializable]
public class MeshData
{
    [SerializeField, HideInInspector] private byte[] m_Data;
    private Mesh m_Mesh;

    public byte[] Data
    {
        get { return m_Data; }
    }

    public void SetMesh(Mesh aMesh)
    {
        m_Mesh = aMesh;
        if (aMesh == null)
            m_Data = null;
        else
            m_Data = MeshSerializer.SerializeMesh(m_Mesh);
    }

    public Mesh GetMesh()
    {
        if (m_Mesh == null && m_Data != null)
            m_Mesh = MeshSerializer.DeserializeMesh(m_Data);
        return m_Mesh;
    }
}


public static class MeshSerializer
{
    /*
     * Structure:
     * - Magic string "Mesh" (4 bytes)
     * - vertex count [int] (4 bytes)
     * - submesh count [int] (4 bytes)
     * - vertices [array of Vector3]
     * 
     * - additional chunks:
     *   [vertex attributes]
     *   - Name (name of the Mesh object)
     *   - Normals [array of Vector3]
     *   - Tangents [array of Vector4]
     *   - Colors [array of Color32]
     *   - UV0-4 [
     *       - component count[byte](2/3/4)
     *       - array of Vector2/3/4
     *     ]
     *   - BoneWeights [array of 4x int+float pair]
     *   
     *   [other data]
     *   - Submesh [
     *       - topology[byte]
     *       - count[int]
     *       - component size[byte](1/2/4)
     *       - array of byte/ushort/int
     *     ]
     *   - Bindposes [
     *       - count[int]
     *       - array of Matrix4x4
     *     ]
     *   - BlendShape [
     *       - Name [string]
     *       - frameCount [int]
     *       - frames [ array of:
     *           - frameWeight [float]
     *           - array of [
     *               - position delta [Vector3]
     *               - normal delta [Vector3]
     *               - tangent delta [Vector3]
     *             ]
     *         ]
     *     ]
     */
    private enum EChunkID : byte
    {
        End,
        Name,
        Normals,
        Tangents,
        Colors,
        BoneWeights,
        UV0,
        UV1,
        UV2,
        UV3,
        Submesh,
        Bindposes,
        BlendShape,
    }

    const uint m_Magic = 0x6873654D; // "Mesh"

    public static int GetMeshDataSize(Mesh.MeshData meshData, bool hasTangents, bool hasColors, bool hasUvs)
    {
        return 0;
    }

    public static byte[] SerializeMesh(Mesh aMesh)
    {
        using (var stream = new MemoryStream())
        {
            SerializeMesh(stream, aMesh);
            return stream.ToArray();
        }
    }

    public static void SerializeMesh(MemoryStream aStream, Mesh aMesh)
    {
        using (var writer = new BinaryWriter(aStream))
            SerializeMesh(writer, aMesh);
    }

    public static byte[] SerializeMeshData(Mesh.MeshData aMesh, bool hasTangents, bool hasColors, bool[] hasUvs,
        bool hasBoneWeights)
    {
        using (var stream = new MemoryStream())
        {
            SerializeMeshData(stream, aMesh, hasTangents, hasColors, hasUvs);
            return stream.ToArray();
        }
    }

    public static void SerializeMeshData(MemoryStream aStream, Mesh.MeshData aMesh, bool hasTangents, bool hasColors,
        bool[] hasUvs)
    {
        using (var writer = new BinaryWriter(aStream))
            SerializeMeshData(writer, aMesh, hasTangents, hasColors, hasUvs);
    }


    public static void SerializeMeshData(BinaryWriter aWriter, Mesh.MeshData aMesh, bool hasTangents, bool hasColors,
        bool[] hasUvs)
    {
        aWriter.Write(m_Magic);
        var vertices = new NativeArray<Vector3>(aMesh.vertexCount, Allocator.TempJob);
        aMesh.GetVertices(vertices);
        int count = vertices.Length;
        int subMeshCount = aMesh.subMeshCount;
        aWriter.Write(count);
        aWriter.Write(subMeshCount);
        foreach (var v in vertices)
            aWriter.WriteVector3(v);
        vertices.Dispose();

        var normals = new NativeArray<Vector3>(aMesh.vertexCount, Allocator.TempJob);
        aMesh.GetNormals(normals);
        if (normals.Length == count)
        {
            aWriter.Write((byte)EChunkID.Normals);
            foreach (var v in normals)
                aWriter.WriteVector3(v);
        }

        normals.Dispose();

        if (hasTangents)
        {
            var tangents = new NativeArray<Vector4>(aMesh.vertexCount, Allocator.TempJob);
            aMesh.GetTangents(tangents);

            if (tangents.Length == count)
            {
                aWriter.Write((byte)EChunkID.Tangents);
                foreach (var v in tangents)
                    aWriter.WriteVector4(v);
            }

            tangents.Dispose();
        }

        if (hasColors)
        {
            var colors = new NativeArray<Color32>(aMesh.vertexCount, Allocator.TempJob);
            aMesh.GetColors(colors);
            if (colors.Length == count)
            {
                aWriter.Write((byte)EChunkID.Colors);
                foreach (var c in colors)
                    aWriter.WriteColor32(c);
            }

            colors.Dispose();
        }


        for (int i = 0; i < 4; i++)
        {
            if (!hasUvs[i])
                continue;

            var uvs = new NativeArray<Vector4>(aMesh.vertexCount, Allocator.TempJob);
            aMesh.GetUVs(i, uvs);
            if (uvs.Length == count)
            {
                aWriter.Write((byte)((byte)EChunkID.UV0 + i));
                byte channelCount = 2;
                foreach (var uv in uvs)
                {
                    if (uv.z != 0f)
                        channelCount = 3;
                    if (uv.w != 0f)
                    {
                        channelCount = 4;
                        break;
                    }
                }

                aWriter.Write(channelCount);
                if (channelCount == 2)
                    foreach (var uv in uvs)
                        aWriter.WriteVector2(uv);
                else if (channelCount == 3)
                    foreach (var uv in uvs)
                        aWriter.WriteVector3(uv);
                else
                    foreach (var uv in uvs)
                        aWriter.WriteVector4(uv);
            }

            uvs.Dispose();
        }


        for (int i = 0; i < subMeshCount; i++)
        {
            var subMeshDesc = aMesh.GetSubMesh(i);
            var indices = new NativeArray<int>(subMeshDesc.indexCount, Allocator.TempJob);
            aMesh.GetIndices(indices, i);
            if (indices.Length > 0)
            {
                aWriter.Write((byte)EChunkID.Submesh);
                aWriter.Write((byte)subMeshDesc.topology);
                aWriter.Write(indices.Length);
                var max = int.MinValue;
                foreach (var index in indices) max = Math.Max(max, index);
                if (max < 256)
                {
                    aWriter.Write((byte)1);
                    foreach (var index in indices)
                        aWriter.Write((byte)index);
                }
                else if (max < 65536)
                {
                    aWriter.Write((byte)2);
                    foreach (var index in indices)
                        aWriter.Write((ushort)index);
                }
                else
                {
                    aWriter.Write((byte)4);
                    foreach (var index in indices)
                        aWriter.Write(index);
                }
            }

            indices.Dispose();
        }

        aWriter.Write((byte)EChunkID.End);
    }

    public static void SerializeMesh(BinaryWriter aWriter, Mesh aMesh)
    {
        aWriter.Write(m_Magic);
        var vertices = aMesh.vertices;
        int count = vertices.Length;
        int subMeshCount = aMesh.subMeshCount;
        aWriter.Write(count);
        aWriter.Write(subMeshCount);
        foreach (var v in vertices)
            aWriter.WriteVector3(v);

        // start of tagged chunks
        if (!string.IsNullOrEmpty(aMesh.name))
        {
            aWriter.Write((byte)EChunkID.Name);
            aWriter.Write(aMesh.name);
        }

        var normals = aMesh.normals;
        if (normals != null && normals.Length == count)
        {
            aWriter.Write((byte)EChunkID.Normals);
            foreach (var v in normals)
                aWriter.WriteVector3(v);
            normals = null;
        }

        var tangents = aMesh.tangents;
        if (tangents != null && tangents.Length == count)
        {
            aWriter.Write((byte)EChunkID.Tangents);
            foreach (var v in tangents)
                aWriter.WriteVector4(v);
            tangents = null;
        }

        var colors = aMesh.colors32;
        if (colors != null && colors.Length == count)
        {
            aWriter.Write((byte)EChunkID.Colors);
            foreach (var c in colors)
                aWriter.WriteColor32(c);
            colors = null;
        }

        var boneWeights = aMesh.boneWeights;
        if (boneWeights != null && boneWeights.Length == count)
        {
            aWriter.Write((byte)EChunkID.BoneWeights);
            foreach (var w in boneWeights)
                aWriter.WriteBoneWeight(w);
            boneWeights = null;
        }

        List<Vector4> uvs = new List<Vector4>();
        for (int i = 0; i < 4; i++)
        {
            uvs.Clear();
            aMesh.GetUVs(i, uvs);
            if (uvs.Count == count)
            {
                aWriter.Write((byte)((byte)EChunkID.UV0 + i));
                byte channelCount = 2;
                foreach (var uv in uvs)
                {
                    if (uv.z != 0f)
                        channelCount = 3;
                    if (uv.w != 0f)
                    {
                        channelCount = 4;
                        break;
                    }
                }

                aWriter.Write(channelCount);
                if (channelCount == 2)
                    foreach (var uv in uvs)
                        aWriter.WriteVector2(uv);
                else if (channelCount == 3)
                    foreach (var uv in uvs)
                        aWriter.WriteVector3(uv);
                else
                    foreach (var uv in uvs)
                        aWriter.WriteVector4(uv);
            }
        }

        List<int> indices = new List<int>(count * 3);
        for (int i = 0; i < subMeshCount; i++)
        {
            indices.Clear();
            aMesh.GetIndices(indices, i);
            if (indices.Count > 0)
            {
                aWriter.Write((byte)EChunkID.Submesh);
                aWriter.Write((byte)aMesh.GetTopology(i));
                aWriter.Write(indices.Count);
                var max = indices.Max();
                if (max < 256)
                {
                    aWriter.Write((byte)1);
                    foreach (var index in indices)
                        aWriter.Write((byte)index);
                }
                else if (max < 65536)
                {
                    aWriter.Write((byte)2);
                    foreach (var index in indices)
                        aWriter.Write((ushort)index);
                }
                else
                {
                    aWriter.Write((byte)4);
                    foreach (var index in indices)
                        aWriter.Write(index);
                }
            }
        }

        var bindposes = aMesh.bindposes;
        if (bindposes != null && bindposes.Length > 0)
        {
            aWriter.Write((byte)EChunkID.Bindposes);
            aWriter.Write(bindposes.Length);
            foreach (var b in bindposes)
                aWriter.WriteMatrix4x4(b);
            bindposes = null;
        }

        int blendShapeCount = aMesh.blendShapeCount;
        if (blendShapeCount > 0)
        {
            var blendVerts = new Vector3[count];
            var blendNormals = new Vector3[count];
            var blendTangents = new Vector3[count];
            for (int i = 0; i < blendShapeCount; i++)
            {
                aWriter.Write((byte)EChunkID.BlendShape);
                aWriter.Write(aMesh.GetBlendShapeName(i));
                var frameCount = aMesh.GetBlendShapeFrameCount(i);
                aWriter.Write(frameCount);
                for (int n = 0; n < frameCount; n++)
                {
                    aMesh.GetBlendShapeFrameVertices(i, n, blendVerts, blendNormals, blendTangents);
                    aWriter.Write(aMesh.GetBlendShapeFrameWeight(i, n));
                    for (int k = 0; k < count; k++)
                    {
                        aWriter.WriteVector3(blendVerts[k]);
                        aWriter.WriteVector3(blendNormals[k]);
                        aWriter.WriteVector3(blendTangents[k]);
                    }
                }
            }
        }

        aWriter.Write((byte)EChunkID.End);
    }


    public static Mesh DeserializeMesh(byte[] aData, Mesh aTarget = null)
    {
        using (var stream = new MemoryStream(aData))
            return DeserializeMesh(stream, aTarget);
    }

    public static Mesh DeserializeMesh(MemoryStream aStream, Mesh aTarget = null)
    {
        using (var reader = new BinaryReader(aStream))
            return DeserializeMesh(reader, aTarget);
    }

    public static Mesh DeserializeMesh(BinaryReader aReader, Mesh aTarget = null)
    {
        if (aReader.ReadUInt32() != m_Magic)
            return null;
        if (aTarget == null)
            aTarget = new Mesh();
        aTarget.Clear();
        aTarget.ClearBlendShapes();
        int count = aReader.ReadInt32();
        if (count > 65534)
            aTarget.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        int subMeshCount = aReader.ReadInt32();
        Vector3[] vector3Array = new Vector3[count];
        Vector3[] vector3Array2 = null;
        Vector3[] vector3Array3 = null;
        List<Vector4> vector4List = null;
        for (int i = 0; i < count; i++)
            vector3Array[i] = aReader.ReadVector3();
        aTarget.vertices = vector3Array;
        aTarget.subMeshCount = subMeshCount;
        int subMeshIndex = 0;
        byte componentCount = 0;

        // reading chunks
        var stream = aReader.BaseStream;
        while ((stream.CanSeek && stream.Position < stream.Length) || stream.CanRead)
        {
            var chunkID = (EChunkID)aReader.ReadByte();
            if (chunkID == EChunkID.End)
                break;
            switch (chunkID)
            {
                case EChunkID.Name:
                    aTarget.name = aReader.ReadString();
                    break;
                case EChunkID.Normals:
                    for (int i = 0; i < count; i++)
                        vector3Array[i] = aReader.ReadVector3();
                    aTarget.normals = vector3Array;
                    break;
                case EChunkID.Tangents:
                    if (vector4List == null)
                        vector4List = new List<Vector4>(count);
                    vector4List.Clear();
                    for (int i = 0; i < count; i++)
                        vector4List.Add(aReader.ReadVector4());
                    aTarget.SetTangents(vector4List);
                    break;
                case EChunkID.Colors:
                    var colors = new Color32[count];
                    for (int i = 0; i < count; i++)
                        colors[i] = aReader.ReadColor32();
                    aTarget.colors32 = colors;
                    break;
                case EChunkID.BoneWeights:
                    var boneWeights = new BoneWeight[count];
                    for (int i = 0; i < count; i++)
                        boneWeights[i] = aReader.ReadBoneWeight();
                    aTarget.boneWeights = boneWeights;
                    break;
                case EChunkID.UV0:
                case EChunkID.UV1:
                case EChunkID.UV2:
                case EChunkID.UV3:
                    int uvChannel = chunkID - EChunkID.UV0;
                    componentCount = aReader.ReadByte();
                    if (vector4List == null)
                        vector4List = new List<Vector4>(count);
                    vector4List.Clear();

                    if (componentCount == 2)
                    {
                        for (int i = 0; i < count; i++)
                            vector4List.Add(aReader.ReadVector2());
                    }
                    else if (componentCount == 3)
                    {
                        for (int i = 0; i < count; i++)
                            vector4List.Add(aReader.ReadVector3());
                    }
                    else if (componentCount == 4)
                    {
                        for (int i = 0; i < count; i++)
                            vector4List.Add(aReader.ReadVector4());
                    }

                    aTarget.SetUVs(uvChannel, vector4List);
                    break;
                case EChunkID.Submesh:
                    var topology = (MeshTopology)aReader.ReadByte();
                    int indexCount = aReader.ReadInt32();
                    var indices = new int[indexCount];
                    componentCount = aReader.ReadByte();
                    if (componentCount == 1)
                    {
                        for (int i = 0; i < indexCount; i++)
                            indices[i] = aReader.ReadByte();
                    }
                    else if (componentCount == 2)
                    {
                        for (int i = 0; i < indexCount; i++)
                            indices[i] = aReader.ReadUInt16();
                    }
                    else if (componentCount == 4)
                    {
                        for (int i = 0; i < indexCount; i++)
                            indices[i] = aReader.ReadInt32();
                    }

                    aTarget.SetIndices(indices, topology, subMeshIndex++, false);
                    break;
                case EChunkID.Bindposes:
                    int bindposesCount = aReader.ReadInt32();
                    var bindposes = new Matrix4x4[bindposesCount];
                    for (int i = 0; i < bindposesCount; i++)
                        bindposes[i] = aReader.ReadMatrix4x4();
                    aTarget.bindposes = bindposes;
                    break;
                case EChunkID.BlendShape:
                    var blendShapeName = aReader.ReadString();
                    int frameCount = aReader.ReadInt32();
                    if (vector3Array2 == null)
                        vector3Array2 = new Vector3[count];
                    if (vector3Array3 == null)
                        vector3Array3 = new Vector3[count];
                    for (int i = 0; i < frameCount; i++)
                    {
                        float weight = aReader.ReadSingle();
                        for (int n = 0; n < count; n++)
                        {
                            vector3Array[n] = aReader.ReadVector3();
                            vector3Array2[n] = aReader.ReadVector3();
                            vector3Array3[n] = aReader.ReadVector3();
                        }

                        aTarget.AddBlendShapeFrame(blendShapeName, weight, vector3Array, vector3Array2, vector3Array3);
                    }

                    break;
            }
        }

        return aTarget;
    }
}


public static class BinaryReaderWriterUnityExt
{
    public static void WriteVector2(this BinaryWriter aWriter, Vector2 aVec)
    {
        aWriter.Write(aVec.x);
        aWriter.Write(aVec.y);
    }

    public static Vector2 ReadVector2(this BinaryReader aReader)
    {
        return new Vector2(aReader.ReadSingle(), aReader.ReadSingle());
    }

    public static void WriteVector3(this BinaryWriter aWriter, Vector3 aVec)
    {
        aWriter.Write(aVec.x);
        aWriter.Write(aVec.y);
        aWriter.Write(aVec.z);
    }

    public static Vector3 ReadVector3(this BinaryReader aReader)
    {
        return new Vector3(aReader.ReadSingle(), aReader.ReadSingle(), aReader.ReadSingle());
    }

    public static void WriteVector4(this BinaryWriter aWriter, Vector4 aVec)
    {
        aWriter.Write(aVec.x);
        aWriter.Write(aVec.y);
        aWriter.Write(aVec.z);
        aWriter.Write(aVec.w);
    }

    public static Vector4 ReadVector4(this BinaryReader aReader)
    {
        return new Vector4(aReader.ReadSingle(), aReader.ReadSingle(), aReader.ReadSingle(), aReader.ReadSingle());
    }

    public static void WriteColor32(this BinaryWriter aWriter, Color32 aCol)
    {
        aWriter.Write(aCol.r);
        aWriter.Write(aCol.g);
        aWriter.Write(aCol.b);
        aWriter.Write(aCol.a);
    }

    public static Color32 ReadColor32(this BinaryReader aReader)
    {
        return new Color32(aReader.ReadByte(), aReader.ReadByte(), aReader.ReadByte(), aReader.ReadByte());
    }

    public static void WriteMatrix4x4(this BinaryWriter aWriter, Matrix4x4 aMat)
    {
        aWriter.Write(aMat.m00);
        aWriter.Write(aMat.m01);
        aWriter.Write(aMat.m02);
        aWriter.Write(aMat.m03);
        aWriter.Write(aMat.m10);
        aWriter.Write(aMat.m11);
        aWriter.Write(aMat.m12);
        aWriter.Write(aMat.m13);
        aWriter.Write(aMat.m20);
        aWriter.Write(aMat.m21);
        aWriter.Write(aMat.m22);
        aWriter.Write(aMat.m23);
        aWriter.Write(aMat.m30);
        aWriter.Write(aMat.m31);
        aWriter.Write(aMat.m32);
        aWriter.Write(aMat.m33);
    }

    public static Matrix4x4 ReadMatrix4x4(this BinaryReader aReader)
    {
        var m = new Matrix4x4();
        m.m00 = aReader.ReadSingle();
        m.m01 = aReader.ReadSingle();
        m.m02 = aReader.ReadSingle();
        m.m03 = aReader.ReadSingle();
        m.m10 = aReader.ReadSingle();
        m.m11 = aReader.ReadSingle();
        m.m12 = aReader.ReadSingle();
        m.m13 = aReader.ReadSingle();
        m.m20 = aReader.ReadSingle();
        m.m21 = aReader.ReadSingle();
        m.m22 = aReader.ReadSingle();
        m.m23 = aReader.ReadSingle();
        m.m30 = aReader.ReadSingle();
        m.m31 = aReader.ReadSingle();
        m.m32 = aReader.ReadSingle();
        m.m33 = aReader.ReadSingle();
        return m;
    }

    public static void WriteBoneWeight(this BinaryWriter aWriter, BoneWeight aWeight)
    {
        aWriter.Write(aWeight.boneIndex0);
        aWriter.Write(aWeight.weight0);
        aWriter.Write(aWeight.boneIndex1);
        aWriter.Write(aWeight.weight1);
        aWriter.Write(aWeight.boneIndex2);
        aWriter.Write(aWeight.weight2);
        aWriter.Write(aWeight.boneIndex3);
        aWriter.Write(aWeight.weight3);
    }

    public static BoneWeight ReadBoneWeight(this BinaryReader aReader)
    {
        var w = new BoneWeight();
        w.boneIndex0 = aReader.ReadInt32();
        w.weight0 = aReader.ReadSingle();
        w.boneIndex1 = aReader.ReadInt32();
        w.weight1 = aReader.ReadSingle();
        w.boneIndex2 = aReader.ReadInt32();
        w.weight2 = aReader.ReadSingle();
        w.boneIndex3 = aReader.ReadInt32();
        w.weight3 = aReader.ReadSingle();
        return w;
    }
}
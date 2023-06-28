using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;


public enum TextureSize
{
    Small = 512,
    Medium = 1024,
    Large = 2048,
    ExtraLarge = 4096
}

public class MeshProcessor : MonoBehaviour
{
    public static MeshProcessor Singleton;

    //meshes that have already been processed, can be used for generating textures
    private SortedDictionary<string, NetworkMesh> _meshes = new();
    public TextureSize textureSize = TextureSize.Large;
    private ConcurrentQueue<Tuple<NetworkMesh, Vector3[], int[]>> _meshesToProcess = new();
    private ConcurrentQueue<Tuple<NetworkMesh, Vector3[], int[], Vector2[]>> _meshesToRejoin = new();
    private ConcurrentQueue<string> _meshesToGenerateTextures = new();
    private ConcurrentQueue<Tuple<string, Texture2D>> _texturesToRejoin = new();

    [DebugGUIGraph()] public float CurrentMeshesToProcess => _meshesToProcess.Count;
    [DebugGUIGraph()] public float CurrentMeshesToRejoin => _meshesToRejoin.Count;
    [DebugGUIGraph()] public float CurrentMeshesToGenerateTextures => _meshesToGenerateTextures.Count;
    [DebugGUIGraph()] public float CurrentTexturesToRejoin => _texturesToRejoin.Count;


    private List<PositionedPhoto> _positionedPhotos = new();

    public UVGenerator UvGenerator;


    public List<ShaderStage> shaderStages = new()
        { ShaderStage.ClearRasterizeTexture, ShaderStage.Rasterize, ShaderStage.ProjectOnMesh };

    private Dictionary<ShaderStage, int> _kernels = new();

    public ComputeShader computeShader;
    private GraphicsBuffer _meshesBuffer;
    private GraphicsBuffer _verticesBuffer;
    private GraphicsBuffer _trianglesBuffer;
    private RenderTexture _rasterizerTexture;
    private GraphicsBuffer _rasterizerMeshHitBuffer;

    static int inputResolutionId = Shader.PropertyToID("inputResolution");
    static int meshTextureResolutionId = Shader.PropertyToID("meshTextureResolution");
    static int worldToCameraMatrixId = Shader.PropertyToID("worldToCameraMatrix");

    static int verticesBufferId = Shader.PropertyToID("vertices");
    static int trianglesBufferId = Shader.PropertyToID("triangles");
    static int meshesBufferId = Shader.PropertyToID("meshes");

    static int rasterizerDepthTextureId = Shader.PropertyToID("rasterizerDepthTexture");
    static int meshHitsTextureId = Shader.PropertyToID("meshHitsTexture");
    static int inputPhotoId = Shader.PropertyToID("inputPhoto");
    static int meshTextureId = Shader.PropertyToID("meshTexture");
    static int meshIdId = Shader.PropertyToID("meshId");

    private bool doRasterize=true;

    private void Start()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }

        StartCoroutine(ProcessMeshes());
    }


    //coroutine that processes meshes
    private IEnumerator ProcessMeshes()
    {
        while (true)
        {
            while (_meshesToProcess.TryDequeue(out var tuple))
            {
                Task.Run(() =>
                {
                    ProcessMesh(tuple.Item2, tuple.Item3, out var newVertices, out var newIndices, out var uvs);
                    if (newVertices.Length == 0)
                    {
                        Debug.LogError(
                            $"No vertices generated, from {tuple.Item2.Length} vertices on mesh {tuple.Item1.name}");
                    }

                    _meshesToRejoin.Enqueue(new(tuple.Item1, newVertices, newIndices, uvs));
                });
            }

            var modifiedMeshes = false;
            while (_meshesToRejoin.TryDequeue(out var tuple))
            {
                Debug.Log("Rejoining mesh");

                var networkMesh = tuple.Item1;
                if (!networkMesh)
                {
                    Debug.LogWarning($"Mesh {tuple.Item1} was deleted during mesh generation");
                    continue;
                }

                var mesh = RejoinMesh(tuple.Item2, tuple.Item3, tuple.Item4);
                networkMesh.SetMesh(mesh);

                if (!_meshes.ContainsKey(networkMesh.name))
                {
                    _meshes.Add(networkMesh.name, networkMesh);
                }

                _meshesToGenerateTextures.Enqueue(networkMesh.name);
                modifiedMeshes = true;
            }

            if (modifiedMeshes)
            {
                //filter disabled meshes
                foreach (var item in _meshes.Where(item => !item.Value).ToList())
                {
                    _meshes.Remove(item.Key);
                }
                SetupMeshBuffers();
            }

            if (doRasterize)
            {
                if (_meshesToGenerateTextures.TryDequeue(out var meshName))
                {
                    Debug.Log("Generating texture");
                    var texture = GenerateTexture(meshName);
                    var width = texture.width;
                    var height = texture.height;
                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    RenderTexture.active = texture;
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    texture.Release();
                    texture.DiscardContents();
                    _texturesToRejoin.Enqueue(new(meshName, tex));
                }
            }

            while (_texturesToRejoin.TryDequeue(out var tuple))
            {
                Debug.Log("Rejoining texture");
                var networkMesh = _meshes[tuple.Item1];
                if (!networkMesh)
                {
                    Debug.LogWarning("Mesh was deleted during texture generation");
                    continue;
                }

                networkMesh.GetComponent<NetworkTexture>().SetTexture(tuple.Item2);
            }

            yield return null;
        }
    }

    public void RemoveMesh(string meshName)
    {
        //remove from all queues
        _meshesToProcess = new(_meshesToProcess.Where(x => x.Item1.gameObject.name != meshName));
        _meshesToRejoin = new(_meshesToRejoin.Where(x => x.Item1.gameObject.name != meshName));
        _meshesToGenerateTextures = new(_meshesToGenerateTextures.Where(x => x != meshName));
        if (_meshes.ContainsKey(meshName))
        {
            _meshes.Remove(meshName);
        }
    }

    /**
     * receive an empty mesh with only vertices and indices
     */
    public void EnqueueMesh(NetworkMesh networkMesh, Vector3[] vertices, int[] indices)
    {
        _meshesToProcess.Enqueue(new(networkMesh, vertices, indices));
    }

    public Mesh RejoinMesh(Vector3[] vertices, int[] indices, Vector2[] uvs)
    {
        var mesh = new Mesh();
        mesh.SetVertexBufferParams(vertices.Length,
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3,
                stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2,
                stream: 0));
        mesh.SetVertices(vertices);
        mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
        mesh.SetTriangles(indices, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public RenderTexture GenerateTexture(string meshName)
    {
        RenderTexture texture = null;
        try
        {
            texture = new RenderTexture((int)textureSize, (int)textureSize, 0, RenderTextureFormat.ARGB32);
            texture.enableRandomWrite = true;
            texture.Create();
            //todo: choose the right photos and necessary meshes
            var id = _meshes.Values.ToList().FindIndex(x => x.name == meshName);
            computeShader.SetInt(meshIdId, id);
            computeShader.SetInts(meshTextureResolutionId, (int)textureSize, (int)textureSize);
            shaderStages.ForEach(stage => computeShader.SetTexture(ShaderStageId(stage), meshTextureId, texture));


            foreach (var photo in _positionedPhotos)
            {
                //perform rasterization from perspective of the photo
                RenderTexture depthDebugTexture = null;
                GraphicsBuffer meshHitBuffer = null;
                try
                {
                    depthDebugTexture =
                        RenderTexture.GetTemporary(photo.Width, photo.Height, 0, RenderTextureFormat.ARGB32);
                    depthDebugTexture.enableRandomWrite = true;
                    depthDebugTexture.Create();

                    meshHitBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None,
                        photo.Width * photo.Height, Marshal.SizeOf(typeof(ComputeBuffer_MeshHit)));

                    shaderStages.ForEach(stage =>
                    {
                        computeShader.SetTexture(ShaderStageId(stage), inputPhotoId, photo.Texture);
                        computeShader.SetTexture(ShaderStageId(stage), rasterizerDepthTextureId, depthDebugTexture);
                        computeShader.SetBuffer(ShaderStageId(stage), meshHitsTextureId, meshHitBuffer);
                    });

                    computeShader.SetInts(inputResolutionId, photo.Width, photo.Height);
                    computeShader.SetMatrix(worldToCameraMatrixId, photo.CombinedMatrix.inverse);

                    //execute the compute shader stages
                    computeShader.Dispatch(ShaderStageId(ShaderStage.ClearRasterizeTexture), photo.Width / 8,
                        photo.Height / 8, 1);
                    computeShader.Dispatch(ShaderStageId(ShaderStage.Rasterize),
                        (int)Math.Ceiling(_trianglesBuffer.count / 64f), 1, 1);
                    computeShader.Dispatch(ShaderStageId(ShaderStage.ProjectOnMesh), photo.Width / 8,
                        photo.Height / 8, 1);

                    /*//create new quad and render the render texture to it
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    var existingQuad = photo.transform.GetChild(0);
                    quad.transform.position = existingQuad.position + Vector3.up * 0.1f;
                    quad.transform.rotation = existingQuad.rotation;
                    quad.transform.localScale = existingQuad.localScale;
                    quad.GetComponent<MeshRenderer>().material.mainTexture = depthDebugTexture;*/
                }
                finally
                {
                    if (depthDebugTexture)
                    {
                        depthDebugTexture.Release();
                        depthDebugTexture.DiscardContents();
                        RenderTexture.ReleaseTemporary(depthDebugTexture);
                    }

                    if (meshHitBuffer != null)
                    {
                        meshHitBuffer.Release();
                        meshHitBuffer.Dispose();
                    }
                }
            }
        }
        catch (Exception e)
        {
            texture?.Release();
            Debug.LogError(e);
            return null;
        }

        return texture;
    }

    private void SetupMeshBuffers()
    {
        _verticesBuffer?.Release();
        _verticesBuffer?.Dispose();
        _trianglesBuffer?.Release();
        _trianglesBuffer?.Dispose();
        _meshesBuffer?.Release();
        _meshesBuffer?.Dispose();

        var meshes = _meshes.Values.Select(nm => nm.GetComponent<MeshFilter>().mesh).ToList();

        var vertexCount = meshes.Sum(m => m.vertexCount);
        var triangleCount = meshes.Sum(m => m.triangles.Length / 3);

        _verticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None,
            vertexCount, Marshal.SizeOf(typeof(ComputeBuffer_Vertex)));
        _trianglesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None,
            triangleCount, Marshal.SizeOf(typeof(ComputeBuffer_Triangle)));
        _meshesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None,
            meshes.Count, Marshal.SizeOf(typeof(ComputeBuffer_Mesh)));

        var meshStructs = new List<ComputeBuffer_Mesh>();
        for (int i = 0; i < meshes.Count; i++)
        {
            var mesh = meshes[i];
            var meshStruct = new ComputeBuffer_Mesh
            {
                index = (uint)i,
                vertexOffset = (i == 0 ? 0 : meshStructs[i - 1].vertexOffset + meshStructs[i - 1].vertexCount),
                triangleOffset = (i == 0 ? 0 : meshStructs[i - 1].triangleOffset + meshStructs[i - 1].triangleCount),
                vertexCount = (uint)mesh.vertexCount,
                triangleCount = (uint)mesh.triangles.Length / 3
            };
            meshStructs.Add(meshStruct);

            //copy meshVertexBuffer to _verticesBuffer at offset meshStruct.vertexOffset
            var meshVertexBuffer = mesh.GetVertexBuffer(0);
            var meshVertices = new ComputeBuffer_Vertex[mesh.vertexCount];
            meshVertexBuffer.GetData(meshVertices); //todo dont copy to cpu, but needs its own gpu stage
            _verticesBuffer.SetData(meshVertices, 0, (int)meshStruct.vertexOffset, (int)meshStruct.vertexCount);


            var meshTrianglesBuffer = mesh.GetIndexBuffer();
            var meshTriangles = new ComputeBuffer_Triangle[mesh.triangles.Length / 3];
            meshTrianglesBuffer.GetData(meshTriangles); //todo dont copy to cpu, but needs its own gpu stage
            _trianglesBuffer.SetData(meshTriangles, 0, (int)meshStruct.triangleOffset, (int)meshStruct.triangleCount);
        }

        _meshesBuffer.SetData(meshStructs);
        shaderStages.ForEach(stage =>
        {
            computeShader.SetBuffer(this.ShaderStageId(stage), verticesBufferId, _verticesBuffer);
            computeShader.SetBuffer(ShaderStageId(stage), trianglesBufferId, _trianglesBuffer);
            computeShader.SetBuffer(ShaderStageId(stage), meshesBufferId, _meshesBuffer);
        });
    }

    int ShaderStageId(ShaderStage stage)
    {
        if (!_kernels.TryGetValue(stage, out var id))
        {
            //manual switch statement
            var stageName = stage switch
            {
                ShaderStage.ClearRasterizeTexture => "clear_rasterizer_texture",
                ShaderStage.Rasterize => "rasterize",
                ShaderStage.ProjectOnMesh => "project_hits_on_texture",
                _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
            };
            id = computeShader.FindKernel(stageName);
            _kernels.Add(stage, id);
        }

        return id;
    }

    /**
     * receive a photo taken by a client at a position and rotation
     */
    public void OnNewPhoto(PositionedPhoto photo)
    {
        //todo filter photos which are older and have an almost identical frustum
        _positionedPhotos.Add(photo);
    }

    public void ProcessMesh(Vector3[] vertices, int[] indices, out Vector3[] newVertices, out int[] newIndices,
        out Vector2[] uvs)
    {
        UvGenerator.GenerateUVsForMesh(vertices, indices, out newVertices, out newIndices, out uvs);
    }

    private void OnGUI()
    {
        //button on bottom left 
        if (GUI.Button(new Rect(10, Screen.height - 50, 100, 50), "Generate Texture"))
        {
            //_meshesToGenerateTextures.Enqueue("Mesh 4F8198E77ED61BB9-91B42239FE7753BC");
            doRasterize = true;
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
struct ComputeBuffer_Vertex
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;
};

[StructLayout(LayoutKind.Sequential)]
struct ComputeBuffer_Triangle
{
    public uint vertexIndex1;
    public uint vertexIndex2;
    public uint vertexIndex3;
};

[StructLayout(LayoutKind.Sequential)]
struct ComputeBuffer_Mesh
{
    public uint index;
    public uint vertexOffset;
    public uint triangleOffset;
    public uint vertexCount;
    public uint triangleCount;
};

[StructLayout(LayoutKind.Sequential)]
struct ComputeBuffer_MeshHit
{
    public Vector2 uv; //the uv coordinate of texture of the mesh that was hit
    public float depth; //the depth of the hit away from the camera
    public uint screenPosX; //the pixel coordinate of the photo
    public uint screenPosY; //the pixel coordinate of the photo
    public uint meshIndex; //the index of the mesh that was hit
};

public enum ShaderStage
{
    ClearRasterizeTexture,
    Rasterize,
    ProjectOnMesh,
    FillTexture
}
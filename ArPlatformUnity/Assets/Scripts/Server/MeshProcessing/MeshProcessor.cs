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

    [DebugGUIGraph(1,0,0)] public float CurrentMeshesToProcess => _meshesToProcess.Count;
    [DebugGUIGraph(0,1,0)] public float CurrentMeshesToRejoin => _meshesToRejoin.Count;
    [DebugGUIGraph(0,0,1)] public float CurrentMeshesToGenerateTextures => _meshesToGenerateTextures.Count;
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
    static int triangleCountId = Shader.PropertyToID("triangleCount");

    private bool doRasterize = false;

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
            var Stopwatch = new Stopwatch();
            Stopwatch.Start();
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

            var elapsed1 = Stopwatch.ElapsedMilliseconds;
            Stopwatch.Restart();
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

            var elapsed2 = Stopwatch.ElapsedMilliseconds;
            Stopwatch.Restart();

            var elapsed3 = Stopwatch.ElapsedMilliseconds;
            Stopwatch.Restart();

            if (doRasterize)
            {
                //filter disabled meshes
                foreach (var item in _meshes.Where(item => !item.Value).ToList())
                {
                    _meshes.Remove(item.Key);
                }

                if (_meshesToGenerateTextures.TryDequeue(out var meshName))
                {
                    //_meshesToGenerateTextures.Enqueue(meshName);
                    Debug.Log("Generating texture");
                    var texture = GenerateTexture(meshName);
                    _texturesToRejoin.Enqueue(new(meshName, texture));
                    var width = texture.width;
                    var height = texture.height;
                    /*AsyncGPUReadback.Request(texture, 0, TextureFormat.RGBA32, (request) =>
                    {
                        if (request.hasError)
                        {
                            Debug.LogError("GPU readback error detected.");
                            return;
                        }
                        Debug.Log($"Texture generated {request.width}x{request.height}");
                    });*/
                }
            }

            var elapsed4 = Stopwatch.ElapsedMilliseconds;
            Stopwatch.Restart();

            while (_texturesToRejoin.TryDequeue(out var tuple))
            {
                Debug.Log("Rejoining texture");
                if (_meshes.TryGetValue(tuple.Item1, out var networkMesh))
                {
                    if (!networkMesh)
                    {
                        Debug.LogWarning("Mesh was deleted during texture generation");
                        continue;
                    }

                    networkMesh.GetComponent<NetworkTexture>().SetTexture(tuple.Item2);
                }
            }

            var elapsed5 = Stopwatch.ElapsedMilliseconds;
            Stopwatch.Stop();

            Debug.Log($"ProcessMeshes: {elapsed1}ms, RejoinMeshes: {elapsed2}ms, SetupMeshBuffers: {elapsed3}ms, GenerateTexture: {elapsed4}ms, RejoinTexture: {elapsed5}ms");

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
        var buffer = mesh.GetIndexBuffer();
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public Texture2D GenerateTexture(string meshName)
    {
        var meshes=_meshes.Values.ToList();
        var targetMesh=meshes.Find(x => x.name == meshName).GetComponent<MeshFilter>().mesh;
        
        var desiredMeshId= meshes.FindIndex(x => x.name == meshName);
        
        
        var outputTexture = new Texture2D((int)textureSize,(int)textureSize, TextureFormat.RGBA32, false);
        if (_positionedPhotos.Count == 0)
        {
            return outputTexture;
        }
        //the temporary texture
        var renderTexture = RenderTexture.GetTemporary((int)textureSize, (int)textureSize, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        // we assume all photos have the same size, so we can use the first one
        var samplePhoto = _positionedPhotos.First();
        
        var depthDebugTexture = RenderTexture.GetTemporary(samplePhoto.Width, samplePhoto.Height, 0, RenderTextureFormat.ARGB32);
        depthDebugTexture.enableRandomWrite = true;
        depthDebugTexture.Create();
        
        var meshHitBuffer=new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None,
            samplePhoto.Width * samplePhoto.Height, Marshal.SizeOf(typeof(ComputeBuffer_MeshHit)));
        
        CommandBuffer commandBuffer = new CommandBuffer();
        commandBuffer.name = "GenerateTexture";
        
        //give clear and rasterize shaders access to the temporary textures
        shaderStages.ForEach(stage =>
        {
            commandBuffer.SetComputeTextureParam(computeShader,ShaderStageId(stage), rasterizerDepthTextureId, depthDebugTexture);
            commandBuffer.SetComputeBufferParam(computeShader,ShaderStageId(stage), meshHitsTextureId, meshHitBuffer);
        });
        
        _positionedPhotos.Where(x=>x.IsMeshInFrustum(targetMesh)).ToList().ForEach(photo =>
        {
            //set params
            commandBuffer.SetComputeTextureParam(computeShader, ShaderStageId(ShaderStage.ProjectOnMesh), inputPhotoId, photo.Texture);
            commandBuffer.SetComputeIntParams(computeShader,inputResolutionId,photo.Width,photo.Height);
            commandBuffer.SetComputeMatrixParam(computeShader, worldToCameraMatrixId, photo.CombinedMatrix.inverse);
            //clear hit texture
            commandBuffer.DispatchCompute(computeShader,ShaderStageId(ShaderStage.ClearRasterizeTexture),photo.Width/8,photo.Height/8,1);
            //rasterize each mesh
            
            meshes.Where(x=>photo.IsMeshInFrustum(x.GetComponent<MeshFilter>().mesh)).Select(x => x.GetComponent<MeshFilter>().mesh).ToList().ForEach(mesh =>
            {
                int meshId = meshes.FindIndex(x => x.GetComponent<MeshFilter>().mesh==mesh);
                mesh.vertexBufferTarget|=GraphicsBuffer.Target.Raw;
                mesh.indexBufferTarget|=GraphicsBuffer.Target.Raw;
                var vertexBuffer = mesh.GetVertexBuffer(0);
                var triangleBuffer = mesh.GetIndexBuffer();
                
                commandBuffer.SetComputeIntParam(computeShader, meshIdId, meshId);
                commandBuffer.SetComputeIntParam(computeShader, triangleCountId, mesh.triangles.Length/3);
                commandBuffer.SetComputeBufferParam(computeShader,ShaderStageId(ShaderStage.Rasterize),verticesBufferId,vertexBuffer);
                commandBuffer.SetComputeBufferParam(computeShader,ShaderStageId(ShaderStage.Rasterize),trianglesBufferId,triangleBuffer);

                Debug.Log($"Rasterizing mesh {meshId} {vertexBuffer.count} {triangleBuffer.count}: {(int)Math.Ceiling(mesh.triangles.Length/3f/64)}");
                commandBuffer.DispatchCompute(computeShader,ShaderStageId(ShaderStage.Rasterize),(int)Math.Ceiling(mesh.triangles.Length/3f/64),1,1);
                
            });
            //give project shader access to the photo texture
            commandBuffer.SetComputeTextureParam(computeShader,ShaderStageId(ShaderStage.ProjectOnMesh),inputPhotoId,photo.Texture);
            commandBuffer.SetComputeIntParams(computeShader,meshTextureResolutionId,(int)textureSize,(int)textureSize);
            //tell project shader for which meshId to generate the texture
            
            commandBuffer.SetComputeIntParam(computeShader,meshIdId,desiredMeshId);
            commandBuffer.SetComputeTextureParam(computeShader,ShaderStageId(ShaderStage.ProjectOnMesh),meshTextureId,renderTexture);
            //project on mesh
            commandBuffer.DispatchCompute(computeShader,ShaderStageId(ShaderStage.ProjectOnMesh),photo.Width/8,photo.Height/8,1);
        });
        //copy render texture to output texture
        commandBuffer.CopyTexture(renderTexture, outputTexture);
        
        //execute command buffer
        Graphics.ExecuteCommandBuffer(commandBuffer);
        
        //release temporary textures
        RenderTexture.ReleaseTemporary(renderTexture);
        meshHitBuffer.Release();
        meshHitBuffer.Dispose();
        RenderTexture.ReleaseTemporary(depthDebugTexture);
        
        return outputTexture;
        
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
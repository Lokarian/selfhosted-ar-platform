using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class Rasterizer : MonoBehaviour
{
    public List<GameObject> Meshes=new List<GameObject>();
    private List<Mesh> _meshes = new List<Mesh>();
    
    public ComputeShader computeShader;
    private GraphicsBuffer _meshesBuffer;
    private GraphicsBuffer _verticesBuffer;
    private GraphicsBuffer _trianglesBuffer;
    private RenderTexture _rasterizerTexture;
    
    static int inputResolutionId = Shader.PropertyToID("inputResolution");
    static int worldToCameraMatrixId = Shader.PropertyToID("worldToCameraMatrix");
    
    static int verticesBufferId = Shader.PropertyToID("vertices");
    static int trianglesBufferId = Shader.PropertyToID("triangles");
    static int meshesBufferId = Shader.PropertyToID("meshes");
    
    static int rasterizerDepthTextureId = Shader.PropertyToID("rasterizerDepthTexture");
    public List<ShaderStage> shaderStages = new() { ShaderStage.ClearRasterizeTexture, ShaderStage.Rasterize };

    private RenderTexture _renderTexture;


    private void Start()
    {
        _renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        _renderTexture.enableRandomWrite = true;
        _renderTexture.Create();
        shaderStages.ForEach(stage =>
            computeShader.SetTexture(ShaderStageId(stage), rasterizerDepthTextureId, _renderTexture));
        computeShader.SetInts(inputResolutionId, 1920, 1080);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.GetComponent<MeshRenderer>().material.mainTexture = _renderTexture;
    }

    private void Update()
    {
        if(_meshes.Count!=Meshes.Count)
        {
            _meshes.Clear();
            foreach (var mesh in Meshes)
            {
                _meshes.Add(mesh.GetComponent<MeshFilter>().mesh);
            }
            SetupMeshes();
        }
        if(_meshes.Count==0)
            return;
        computeShader.SetMatrix(worldToCameraMatrixId, (Camera.main.cameraToWorldMatrix*Camera.main.projectionMatrix.inverse).inverse);
        computeShader.Dispatch(ShaderStageId(ShaderStage.ClearRasterizeTexture), 1920 / 8, 1080 / 8,
            1);
        computeShader.Dispatch(ShaderStageId(ShaderStage.Rasterize), (int)Math.Ceiling(_trianglesBuffer.count / 64f), 1, 1);

    }

    private void SetupMeshes()
    {
        var meshes = _meshes;
        var vertexCount = meshes.Sum(m => m.vertexCount);
        var triangleCount = meshes.Sum(m => m.triangles.Length/3);
        
        Debug.Log($"Vertex count: {vertexCount}, triangle count: {triangleCount}");
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
            var meshVertexBuffer = mesh.GetVertexBuffer(0);
            //copy meshVertexBuffer to _verticesBuffer at offset meshStruct.vertexOffset
            var meshVertices = new ComputeBuffer_Vertex[mesh.vertexCount];
            meshVertexBuffer.GetData(meshVertices); //todo dont copy to cpu
            _verticesBuffer.SetData(meshVertices, 0, (int)meshStruct.vertexOffset, (int)meshStruct.vertexCount);


            var meshTrianglesBuffer = mesh.GetIndexBuffer();
            var meshTriangles = new ComputeBuffer_Triangle[mesh.triangles.Length / 3];
            meshTrianglesBuffer.GetData(meshTriangles); //todo dont copy to cpu
            _trianglesBuffer.SetData(meshTriangles, 0, (int)meshStruct.triangleOffset, (int)meshStruct.triangleCount);
        }
        
        _meshesBuffer.SetData(meshStructs);
        shaderStages.ForEach(stage =>
        {
            computeShader.SetBuffer(ShaderStageId(stage), verticesBufferId, _verticesBuffer);
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
                _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
            };
            id = computeShader.FindKernel(stageName);
            _kernels.Add(stage, id);
        }

        return id;
    }
    private Dictionary<ShaderStage, int> _kernels = new();
}

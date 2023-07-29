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
    }

    private void Update()
    {
    }

}

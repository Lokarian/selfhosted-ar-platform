using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using XAtlasSharp;
using Debug = UnityEngine.Debug;
using Mesh = UnityEngine.Mesh;
using IndexFormat = UnityEngine.Rendering.IndexFormat;

public class UVGenerator : MonoBehaviour
{
    public bool GenerateUVsForMesh(Vector3[] vertices,int[] indices,out Vector3[] verticesOut,out int[] indicesOut,out Vector2[] uvsOut) 
    {
        // Create the atlas object
        Atlas atlas = Atlas.Create();

        var indexBufferHandle = GCHandle.Alloc(indices, GCHandleType.Pinned);
        var vertexBufferHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
        
        MeshDecl meshDecl = new MeshDecl();
        meshDecl.VertexCount = (uint)vertices.Length;
        meshDecl.VertexPositionData = vertexBufferHandle.AddrOfPinnedObject();
        meshDecl.VertexPositionStride = sizeof(float) * 3;
        
        meshDecl.IndexCount = (uint)indices.Length;
        meshDecl.IndexData = indexBufferHandle.AddrOfPinnedObject();
        meshDecl.IndexFormat = XAtlasSharp.IndexFormat.UInt32;
        
        AddMeshError result = atlas.AddMesh(meshDecl);

        if (result != AddMeshError.Success)
        {
            Debug.LogError("Failed to add mesh to atlas: " + result.ToString());
            verticesOut = null;
            indicesOut = null;
            uvsOut = null;
            return false;
        }

        // Generate the atlas
        atlas.Generate();

        // Get the mesh data
        XAtlasSharp.Mesh mesh;
        using var meshEnumerator = atlas.Meshes.GetEnumerator();
        meshEnumerator.MoveNext();
        mesh = meshEnumerator.Current;
        verticesOut = new Vector3[mesh.VertexCount];
        uvsOut = new Vector2[mesh.VertexCount];
        indicesOut = mesh.Indices;
        var j = 0;
        foreach (var meshVertex in mesh.Vertices)
        {
            var index = meshVertex.Xref;
            verticesOut[j]=vertices[index];
            uvsOut[j]=new Vector2(meshVertex.Uv[0],meshVertex.Uv[1]);
            j++;
        }
        
        NormalizeUvs(uvsOut);
        
        atlas.Destroy();
        vertexBufferHandle.Free();
        indexBufferHandle.Free();

        return true;
    }

    static void NormalizeUvs(Vector2[] uvs)
    {
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var uv in uvs)
        {
            min.x = Mathf.Min(min.x, uv.x);
            min.y = Mathf.Min(min.y, uv.y);
            max.x = Mathf.Max(max.x, uv.x);
            max.y = Mathf.Max(max.y, uv.y);
        }

        var range = max - min;
        for (var i = 0; i < uvs.Length; i++)
        {
            var uv = uvs[i];
            uv.x = (uv.x - min.x) / range.x;
            uv.y = (uv.y - min.y) / range.y;
            uvs[i] = uv;
        }
        
    }

}


public class XAtlasTest
{
    private static bool s_verbose;
    private static Stopwatch stopwatch;

    static int PrintCallback(string format)
    {
        Debug.Log("\r");
        Debug.Log(format);
        return 0;
    }

    static bool ProgressCallback(ProgressCategory category, int progress, IntPtr userData)
    {
        // Don't interrupt verbose printing.
        if (s_verbose)
            return true;

        if (progress == 0)
            stopwatch.Restart();

        Debug.Log($"\r   {XAtlas.StringForEnum(category)} {progress}%");
        for (int i = 0; i < 10; i++)
            Debug.Log(progress / ((i + 1) * 10) > 0 ? "*" : " ");
        Debug.Log($" {progress}%]");

        if (progress == 100)
            Debug.Log(
                $"\n      {stopwatch.Elapsed.TotalSeconds:0.00} seconds ({stopwatch.ElapsedMilliseconds} ms) elapsed");
        return true;
    }

    public static Mesh Test(Mesh inputMesh)
    {
        // Create empty atlas.
        XAtlas.SetPrint(PrintCallback, s_verbose);
        Atlas atlas = Atlas.Create();

        // Set progress callback.
        stopwatch = new Stopwatch();
        atlas.SetProgressCallback(ProgressCallback, IntPtr.Zero);

        var globalStopwatch = Stopwatch.StartNew();

        // Add meshes to atlas.
        uint totalVertices = 0, totalFaces = 0;

        int meshIndex = 0;
        var indexBuffer = inputMesh.triangles;
        var vertexBuffer = inputMesh.vertices;
        
        
        
        var indexBufferHandle = GCHandle.Alloc(indexBuffer, GCHandleType.Pinned);
        var vertexBufferHandle = GCHandle.Alloc(vertexBuffer, GCHandleType.Pinned);

        var meshDecl = new MeshDecl();
        meshDecl.VertexCount = (uint)vertexBuffer.Length;
        meshDecl.VertexPositionData = vertexBufferHandle.AddrOfPinnedObject();
        meshDecl.VertexPositionStride = sizeof(float) * 3;

        

        meshDecl.IndexCount = (uint)indexBuffer.Length;
        meshDecl.IndexData = indexBufferHandle.AddrOfPinnedObject();
        meshDecl.IndexFormat = XAtlasSharp.IndexFormat.UInt32;


        var error = atlas.AddMesh(meshDecl, 1);
        if (error != AddMeshError.Success)
        {
            atlas.Destroy();
            Debug.Log($"Error adding mesh {meshIndex} '{inputMesh.name}': {XAtlas.StringForEnum(error)}\n");
            return null;
        }

        meshIndex++;
        totalVertices += meshDecl.VertexCount;
        if (meshDecl.FaceCount > 0)
            totalFaces += meshDecl.FaceCount;
        else
            totalFaces += meshDecl.IndexCount / 3; // Assume triangles if MeshDecl.FaceCount not specified.


        //atlas.AddMeshJoin(); // Not necessary. Only called here so geometry totals are printed after the AddMesh progress indicator.
        Debug.Log($"   {totalVertices} total vertices");
        Debug.Log($"   {totalFaces} total faces");

        // Generate atlas.
        Debug.Log("Generating atlas");
        atlas.Generate();
        Debug.Log($"   {atlas.ChartCount} charts");
        Debug.Log($"   {atlas.AtlasCount} atlases");
        int i;
        for (i = 0; i < atlas.AtlasCount; i++)
        {
            Debug.Log($"      {i}: {atlas.Utilization[i] * 100.0f:0.00}% utilization");
        }

        Debug.Log($"   {atlas.Width}x{atlas.Height} resolution");
        totalVertices = 0;

        globalStopwatch.Stop();

        Debug.Log($"   {totalVertices} total vertices");
        Debug.Log(
            $"{globalStopwatch.Elapsed.TotalSeconds} seconds ({globalStopwatch.ElapsedMilliseconds} ms) elapsed total");

        
        //construct new Mesh
        XAtlasSharp.Mesh mesh;
        using var meshEnumerator = atlas.Meshes.GetEnumerator();
        meshEnumerator.MoveNext();
        mesh = meshEnumerator.Current;
        Vector3[] vertices = new Vector3[mesh.VertexCount];
        Vector2[] uvs = new Vector2[mesh.VertexCount];
        
        var j = 0;
        foreach (var meshVertex in mesh.Vertices)
        {
            var index = meshVertex.Xref;
            vertices[j]=vertexBuffer[index];
            uvs[j]=new Vector2(meshVertex.Uv[0],meshVertex.Uv[1]);
            j++;
        }
        
        var outputMesh = new Mesh();
        outputMesh.vertices = vertices;
        NormalizeUvs(uvs);
        outputMesh.uv = uvs;
        outputMesh.triangles = indexBuffer;
        outputMesh.triangles=mesh.Indices;
        
        
        // Cleanup.
        indexBufferHandle.Free();
        vertexBufferHandle.Free();
        atlas.Destroy();
        Debug.Log("Done");

        return outputMesh;
    }

    //modify the uvs to be in the range of 0-1
    static void NormalizeUvs(Vector2[] uvs)
    {
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var uv in uvs)
        {
            min.x = Mathf.Min(min.x, uv.x);
            min.y = Mathf.Min(min.y, uv.y);
            max.x = Mathf.Max(max.x, uv.x);
            max.y = Mathf.Max(max.y, uv.y);
        }

        var range = max - min;
        for (var i = 0; i < uvs.Length; i++)
        {
            var uv = uvs[i];
            uv.x = (uv.x - min.x) / range.x;
            uv.y = (uv.y - min.y) / range.y;
            uvs[i] = uv;
        }
        
    }
}
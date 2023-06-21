using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


public enum TextureSize
{
    Small = 512,
    Medium = 1024,
    Large = 2048,
    ExtraLarge = 4096
}

public class MeshProcessor : MonoBehaviour
{
    private List<NetworkMesh> _meshes = new();
    public TextureSize textureSize = TextureSize.Large;
    private Queue<NetworkMesh> _meshesToProcess = new();
    private Queue<Tuple<NetworkMesh, Vector3[], int[], Vector2[]>> _meshesToRejoin = new();
    private Queue<NetworkMesh> _meshesToGenerateTextures = new();

    public UVGenerator UvGenerator;

    private void Start()
    {
        StartCoroutine(ProcessMeshes());
    }

    //coroutine that processes meshes
    private IEnumerator ProcessMeshes()
    {
        while (true)
        {
            while (_meshesToProcess.TryDequeue(out var networkMesh))
            {
                Debug.Log("Processing mesh");
                var mesh = networkMesh.GetComponent<MeshFilter>().mesh;
                var vertices = mesh.vertices;
                var indices = mesh.triangles;
                Task.Run(() =>
                {
                    ProcessMesh(vertices, indices, out var newVertices, out var newIndices, out var uvs);
                    _meshesToRejoin.Enqueue(new(networkMesh, newVertices, newIndices, uvs));
                });
            }

            while (_meshesToRejoin.TryDequeue(out var tuple))
            {
                Debug.Log("Rejoining mesh");
                var mesh = tuple.Item1.GetComponent<MeshFilter>().mesh;
                mesh.Clear();
                mesh.SetVertices(tuple.Item2);
                mesh.SetTriangles(tuple.Item3, 0);
                mesh.SetUVs(0, tuple.Item4);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                _meshes.Add(tuple.Item1);
                _meshesToGenerateTextures.Enqueue(tuple.Item1);
            }


            yield return null;
        }
    }

    /**
     * receive an empty mesh with only vertices and indices
     */
    public void OnNewMesh(NetworkMesh networkMesh)
    {
        _meshesToProcess.Enqueue(networkMesh);
        /*var texture = GenerateTexture();
        networkMesh.GetComponent<NetworkTexture>().SetTexture(texture);*/
    }


    public Texture2D GenerateTexture()
    {
        var texture = new Texture2D((int)textureSize, (int)textureSize, TextureFormat.RGB24, false);
        var colorArrayRed = new Color[texture.width * texture.height];
        for (int i = 0; i < colorArrayRed.Length; i++)
        {
            colorArrayRed[i] = Color.white;
        }

        texture.SetPixels(colorArrayRed);
        texture.Apply();
        return texture;
    }

    public void RemoveMesh(Mesh mesh)
    {
        //todo implement
    }

    /**
     * receive a photo taken by a client at a position and rotation
     */
    public void OnNewPhoto(Texture2D texture, Matrix4x4 transform)
    {
        //todo implement
    }

    public void ProcessMesh(Vector3[] vertices, int[] indices, out Vector3[] newVertices, out int[] newIndices,
        out Vector2[] uvs)
    {
        UvGenerator.GenerateUVsForMesh(vertices, indices, out newVertices, out newIndices, out uvs);
    }


    public static void CreateVertexForEachTriangle(Vector3[] vertices, int[] triangles, out Vector3[] newVertices,
        out int[] newTriangles)
    {
        // Create new arrays for modified vertices and triangles
        newVertices = new Vector3[triangles.Length];
        newTriangles = new int[triangles.Length];

        // Duplicate vertices based on triangle indices
        for (int i = 0; i < triangles.Length; i++)
        {
            newVertices[i] = vertices[triangles[i]];
            newTriangles[i] = i;
        }
    }
}

class UvCalculator
{
    private enum Facing
    {
        Up,
        Forward,
        Right
    };

    public static Vector2[] CalculateUVs(Vector3[] v /*vertices*/, float scale)
    {
        var uvs = new Vector2[v.Length];

        for (int i = 0; i < uvs.Length - 2; i += 3)
        {
            int i0 = i;
            int i1 = i + 1;
            int i2 = i + 2;

            Vector3 v0 = v[i0];
            Vector3 v1 = v[i1];
            Vector3 v2 = v[i2];

            Vector3 side1 = v1 - v0;
            Vector3 side2 = v2 - v0;
            var direction = Vector3.Cross(side1, side2);
            var facing = FacingDirection(direction);
            switch (facing)
            {
                case Facing.Forward:
                    uvs[i0] = ScaledUV(v0.x, v0.y, scale);
                    uvs[i1] = ScaledUV(v1.x, v1.y, scale);
                    uvs[i2] = ScaledUV(v2.x, v2.y, scale);
                    break;
                case Facing.Up:
                    uvs[i0] = ScaledUV(v0.x, v0.z, scale);
                    uvs[i1] = ScaledUV(v1.x, v1.z, scale);
                    uvs[i2] = ScaledUV(v2.x, v2.z, scale);
                    break;
                case Facing.Right:
                    uvs[i0] = ScaledUV(v0.y, v0.z, scale);
                    uvs[i1] = ScaledUV(v1.y, v1.z, scale);
                    uvs[i2] = ScaledUV(v2.y, v2.z, scale);
                    break;
            }
        }

        return uvs;
    }

    private static bool FacesThisWay(Vector3 v, Vector3 dir, Facing p, ref float maxDot, ref Facing ret)
    {
        float t = Vector3.Dot(v, dir);
        if (t > maxDot)
        {
            ret = p;
            maxDot = t;
            return true;
        }

        return false;
    }

    private static Facing FacingDirection(Vector3 v)
    {
        var ret = Facing.Up;
        float maxDot = Mathf.NegativeInfinity;

        if (!FacesThisWay(v, Vector3.right, Facing.Right, ref maxDot, ref ret))
            FacesThisWay(v, Vector3.left, Facing.Right, ref maxDot, ref ret);

        if (!FacesThisWay(v, Vector3.forward, Facing.Forward, ref maxDot, ref ret))
            FacesThisWay(v, Vector3.back, Facing.Forward, ref maxDot, ref ret);

        if (!FacesThisWay(v, Vector3.up, Facing.Up, ref maxDot, ref ret))
            FacesThisWay(v, Vector3.down, Facing.Up, ref maxDot, ref ret);

        return ret;
    }

    private static Vector2 ScaledUV(float uv1, float uv2, float scale)
    {
        return new Vector2(uv1 / scale, uv2 / scale);
    }
}
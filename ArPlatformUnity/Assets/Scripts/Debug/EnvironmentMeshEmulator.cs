using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class EnvironmentMeshEmulator : MonoBehaviour
{
    public string StoreLocation = "C:/temp/arplatform/unityStorage/environmentMeshes/";
    public GameObject NetworkMeshPrefab;
    public GameObject AlternativeMesh;
    
    public bool LoadOnStart = true;
    public bool SingleMeshMode = false;
    private bool _importedMeshes = false;
    private bool _importedPhotos = false;
    private void Start()
    {
        if (LoadOnStart)
        {
            //after 1 second, load all meshes
            LoadMeshes();
        }
    }

    public void StoreEnvironmentMeshes()
    {
        var networkMeshes = GameObject.FindObjectsOfType<NetworkMesh>();
        foreach (var networkMesh in networkMeshes)
        {
            if (networkMesh.GetMeshRepresentation(out var verices, out var triangles))
            {
                //store raw triangles and vertices
                var meshName = networkMesh.gameObject.name;
                var meshPath = $"{StoreLocation}{meshName}";
                System.IO.Directory.CreateDirectory(meshPath);
                System.IO.File.WriteAllBytes($"{meshPath}/vertices",
                    verices.Select(v => new[] { v.x, v.y, v.z }).SelectMany(v => v)
                        .SelectMany(v => System.BitConverter.GetBytes(v)).ToArray());
                System.IO.File.WriteAllBytes($"{meshPath}/triangles",
                    triangles.SelectMany(v => System.BitConverter.GetBytes(v)).ToArray());
            }
        }
    }

    public void LoadMeshes()
    {
        if (AlternativeMesh != null)
        {
            //FindObjectOfType<MeshProcessor>().ProcessMesh(AlternativeMesh.GetComponent<MeshFilter>().mesh);
            return;
        }
        //loop over all folder, request the mesh and apply it
        foreach (var meshPath in System.IO.Directory.GetDirectories(StoreLocation))
        {
            if (SingleMeshMode&&!meshPath.Contains("EnvironmentNetworkMesh_Mesh 420F8852C6BAB179-738D0BADE72BB38A"))
            {
                continue;
            }
            var meshName = System.IO.Path.GetFileName(meshPath);
            GameObject go = Instantiate(NetworkMeshPrefab, Vector3.zero, Quaternion.identity);
            go.name = meshName;
            go.GetComponent<NetworkObject>().Spawn();
            var networkMesh = go.GetComponent<NetworkMesh>();
            var vertices = System.IO.File.ReadAllBytes($"{meshPath}/vertices");
            var triangles = System.IO.File.ReadAllBytes($"{meshPath}/triangles");
            //use bitconverter to convert the bytes back to vectors3 and int[]
            var verticesFloat = new float[vertices.Length / 4];
            var trianglesInt = new int[triangles.Length / 4];
            for (int i = 0; i < vertices.Length; i += 4)
            {
                verticesFloat[i / 4] = System.BitConverter.ToSingle(vertices, i);
            }
            //vertices to vector3 array
            var verticesVector3 = new Vector3[verticesFloat.Length / 3];
            for (int i = 0; i < verticesFloat.Length; i += 3)
            {
                verticesVector3[i / 3] = new Vector3(verticesFloat[i], verticesFloat[i + 1], verticesFloat[i + 2]);
            }
            //triangles to int array
            for (int i = 0; i < triangles.Length; i += 4)
            {
                trianglesInt[i / 4] = System.BitConverter.ToInt32(triangles, i);
            }
            //networkMesh.SetMesh(verticesVector3.ToList(), trianglesInt.ToList());
            FindObjectOfType<MeshProcessor>().EnqueueMesh(networkMesh, verticesVector3, trianglesInt);
        }
    }

    public void LoadPhotos()
    {
        
    }
    
    //ongui button to store the meshes
    private void OnGUI()
    {
        if(LoadOnStart)
            return;
        /*if (GUI.Button(new Rect(10, 10, 100, 20), "Store Environment Meshes"))
        {
            StoreEnvironmentMeshes();
        }*/
        if (!_importedMeshes)
        {
            if (GUI.Button(new Rect(10, 10, 100, 20), "Load Meshes"))
            {
                LoadMeshes();
            }
        }

        if (_importedMeshes && !_importedPhotos)
        {
            if (GUI.Button(new Rect(10, 10, 100, 20), "Load Photos"))
            {
                LoadPhotos();
            }
        }
    }

}
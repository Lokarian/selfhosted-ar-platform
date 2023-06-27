using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class EnvironmentMeshEmulator : MonoBehaviour
{
    public string StoreLocation = "C:/temp/arplatform/unityStorage/environmentMeshes/";
    public string PhotoStoreLocation = "C:/temp/arplatform/unityStorage/positionedPhotos/";
    public GameObject NetworkMeshPrefab;
    public GameObject PositionedPhotoPrefab;
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
            
            var yOffSet = -0.5f;
            var xOffSet = -1f;
           // verticesVector3 = new[] { new Vector3(0+xOffSet, -0.75f+yOffSet, 2.4f), new Vector3(-0.25f+xOffSet, -0.25f+yOffSet, 2.4f), new Vector3(-0.5f+xOffSet, -0.75f+yOffSet, 2.4f) };
            //trianglesInt = new[] { 0, 2, 1 };
            FindObjectOfType<MeshProcessor>().EnqueueMesh(networkMesh, verticesVector3, trianglesInt);
        }
        _importedMeshes = true;
    }
    public void StorePositionedPhotos()
    {
        //store ProjectionMatrix, CameraMatrix and the mainTexture
        var positionedPhotos = GameObject.FindObjectsOfType<PositionedPhoto>();
        var counter = 0;
        foreach (var positionedPhoto in positionedPhotos)
        {
            var meshName = positionedPhoto.gameObject.name+(counter++);
            var meshPath = $"{PhotoStoreLocation}{meshName}";
            System.IO.Directory.CreateDirectory(meshPath);
            var projectionMatrix = positionedPhoto.ProjectionMatrix;
            var cameraMatrix = positionedPhoto.CameraMatrix;
            var texture = positionedPhoto.GetComponentInChildren<MeshRenderer>().material.mainTexture as Texture2D;
            //store as json
            System.IO.File.WriteAllText($"{meshPath}/projectionMatrix", JsonUtility.ToJson(projectionMatrix));
            System.IO.File.WriteAllText($"{meshPath}/cameraMatrix", JsonUtility.ToJson(cameraMatrix));
            System.IO.File.WriteAllBytes($"{meshPath}/width", System.BitConverter.GetBytes(texture.width));
            System.IO.File.WriteAllBytes($"{meshPath}/height", System.BitConverter.GetBytes(texture.height));
            System.IO.File.WriteAllBytes($"{meshPath}/texture", texture.EncodeToPNG());
        }
    }
    public void LoadPhotos()
    {
        //loop over all folder, request the mesh and apply it
        foreach (var photoPath in System.IO.Directory.GetDirectories(PhotoStoreLocation))
        {
            var photoName = System.IO.Path.GetFileName(photoPath);
            GameObject go = Instantiate(PositionedPhotoPrefab, Vector3.zero, Quaternion.identity);
            go.name = photoName;
            
            var positionedPhoto = go.GetComponent<PositionedPhoto>();
            var projectionMatrix = JsonUtility.FromJson<Matrix4x4>(System.IO.File.ReadAllText($"{photoPath}/projectionMatrix"));
            var cameraMatrix = JsonUtility.FromJson<Matrix4x4>(System.IO.File.ReadAllText($"{photoPath}/cameraMatrix"));
            var width = System.BitConverter.ToInt32(System.IO.File.ReadAllBytes($"{photoPath}/width"),0);
            var height = System.BitConverter.ToInt32(System.IO.File.ReadAllBytes($"{photoPath}/height"),0);
            
            var texture = new Texture2D(width,height);
            texture.LoadImage(System.IO.File.ReadAllBytes($"{photoPath}/texture"));
            positionedPhoto.Initialize(projectionMatrix,cameraMatrix,width,height,texture);
        }
        _importedPhotos = true;
    }
    
    //ongui button to store the meshes
    private void OnGUI()
    {
        if(LoadOnStart)
            return;
        if (GUI.Button(new Rect(10, 50, 100, 20), "Store"))
        {
            StoreEnvironmentMeshes();
            StorePositionedPhotos();
        }
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
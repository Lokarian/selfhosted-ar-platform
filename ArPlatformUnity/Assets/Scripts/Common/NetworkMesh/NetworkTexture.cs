using UnityEngine;

public class NetworkTexture : MonoBehaviour
{
    private Texture2D _texture;
    //public getter
    public Texture2D Texture
    {
        get => _texture;
        private set => _texture = value;
    }
    public NetworkMesh NetworkMesh;
    public int CurrentVersion = 0;
    private Mesh _waitingMesh;

    

    public void SetTexture(Texture2D texture,int? version=null)
    {
        if (NetworkMesh.SyncWithNetworkTexture&&CurrentVersion!=NetworkMesh.CurrentVersion)
        {
            Debug.Log("Waiting for mesh to update");
            _waitingMesh = NetworkMesh.GetComponent<MeshFilter>().mesh;
            return;
        }
        Texture = texture;
        Debug.Log("Setting texture, color at 0,0: "+texture.GetPixel(0,0));
        GetComponent<MeshRenderer>().material.mainTexture = Texture;
    }
    
    
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTexture : MonoBehaviour
{
    public Texture2D _texture;
    
    public void SetTexture(Texture2D texture)
    {
        _texture = texture;
        Debug.Log("Setting texture, color at 0,0: "+texture.GetPixel(0,0));
        GetComponent<MeshRenderer>().material.mainTexture = _texture;
    }
    
    
}

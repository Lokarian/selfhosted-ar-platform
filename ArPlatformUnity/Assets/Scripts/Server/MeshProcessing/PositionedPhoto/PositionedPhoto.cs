using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionedPhoto : MonoBehaviour
{
    public Matrix4x4 ProjectionMatrix;
    
    public void Initialize(Matrix4x4 projectionMatrix,Matrix4x4 cameraToWorldMatrix,int width,int height,Texture2D texture)
    {
        ProjectionMatrix = projectionMatrix;
        Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
        Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));
        transform.position = position;
        transform.rotation = rotation;
        
        GetComponentInChildren<MeshRenderer>().material.mainTexture = texture;
    }

    
    public Texture2D GetTexture()
    {
        return (Texture2D) GetComponentInChildren<MeshRenderer>().material.mainTexture;
    }

    private void OnDrawGizmos()
    {
        //draw frustum based on projection matrix
        Gizmos.color = Color.red;
        Gizmos.matrix = ProjectionMatrix;
        Gizmos.DrawFrustum(Vector3.zero, 60, 1, 0.1f, 1.5f);
    }
}

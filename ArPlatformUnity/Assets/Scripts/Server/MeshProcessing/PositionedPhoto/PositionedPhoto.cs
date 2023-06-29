using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionedPhoto : MonoBehaviour
{
    public Matrix4x4 ProjectionMatrix;
    public Matrix4x4 CameraMatrix;
    public Matrix4x4 CombinedMatrix;
    public Texture2D Texture;
    public Plane[] Planes;
    public float QuadZDistance = 0.75f;
    public bool DrawGizmos = true;

    public int Width;
    public int Height;
    
    [Range(-10,10)]
    public float X;
    [Range(-10,10)]
    public float Y;
    [Range(-10,10)]
    public float Z;

    public void Initialize(Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix, int width, int height,
        Texture2D texture)
    {
        Width = width;
        Height = height;
        ProjectionMatrix = projectionMatrix;
        CameraMatrix = cameraToWorldMatrix;
        CombinedMatrix = CameraMatrix * projectionMatrix.inverse;
        var newPos = (Vector3)cameraToWorldMatrix.GetColumn(3);
        var rotation = Quaternion.LookRotation(cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));
        transform.position = newPos;
        transform.rotation = rotation;

        var quadDistance = Vector3.Distance(transform.position,
            CombinedMatrix.MultiplyPoint(new Vector3(0, 0, QuadZDistance)));
        transform.GetChild(0).transform.localPosition = new Vector3(0, 0, -quadDistance);
        var scaleWidth = Vector3.Distance(CombinedMatrix.MultiplyPoint(new Vector3(-1, 0, QuadZDistance)),
            CombinedMatrix.MultiplyPoint(new Vector3(1, 0, QuadZDistance)));
        var scaleHeight = Vector3.Distance(CombinedMatrix.MultiplyPoint(new Vector3(0, -1, QuadZDistance)),
            CombinedMatrix.MultiplyPoint(new Vector3(0, 1, QuadZDistance)));
        transform.GetChild(0).transform.localScale = new Vector3(scaleWidth, scaleHeight, 1);

        GetComponentInChildren<MeshRenderer>().material.mainTexture = texture;
        Texture = texture;
        Planes = GeometryUtility.CalculateFrustumPlanes(CombinedMatrix.inverse);
        MeshProcessor.Singleton.OnNewPhoto(this);
    }


    public Texture2D GetTexture()
    {
        return (Texture2D)GetComponentInChildren<MeshRenderer>().material.mainTexture;
    }

    private void OnDrawGizmos()
    {
        if (!DrawGizmos)
        {
            return;
        }
        //draw the frustum
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(CombinedMatrix.MultiplyPoint(new Vector3(X,Y,Z)),0.02f);
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(-1, -1, -1)),
            CombinedMatrix.MultiplyPoint(new Vector3(-1, -1, 1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(-1, -1, -1)),
            CombinedMatrix.MultiplyPoint(new Vector3(-1, 1, -1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(-1, -1, -1)),
            CombinedMatrix.MultiplyPoint(new Vector3(1, -1, -1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(-1, -1, 1)),
            CombinedMatrix.MultiplyPoint(new Vector3(-1, 1, 1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(-1, -1, 1)),
            CombinedMatrix.MultiplyPoint(new Vector3(1, -1, 1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(-1, 1, -1)),
            CombinedMatrix.MultiplyPoint(new Vector3(-1, 1, 1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(-1, 1, -1)),
            CombinedMatrix.MultiplyPoint(new Vector3(1, 1, -1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(-1, 1, 1)),
            CombinedMatrix.MultiplyPoint(new Vector3(1, 1, 1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(1, -1, -1)),
            CombinedMatrix.MultiplyPoint(new Vector3(1, -1, 1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(1, -1, -1)),
            CombinedMatrix.MultiplyPoint(new Vector3(1, 1, -1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(1, -1, 1)),
            CombinedMatrix.MultiplyPoint(new Vector3(1, 1, 1)));
        Gizmos.DrawLine(CombinedMatrix.MultiplyPoint(new Vector3(1, 1, -1)),
            CombinedMatrix.MultiplyPoint(new Vector3(1, 1, 1)));
        
        Gizmos.color = Color.red;
        foreach (var plane in Planes)
        {
            //get two perpendicular vectors on the plane
            var v1 = Vector3.Normalize(Vector3.Cross(plane.normal, Vector3.up));
            var v2 = Vector3.Normalize(Vector3.Cross(plane.normal, v1));
            //draw a rectangle on the plane with center at plane.normal*plane.distance
            //Gizmos.DrawLine(plane.normal * plane.distance + v1 + v2,
            //    plane.normal * plane.distance + v1 - v2);
            //Gizmos.DrawLine(plane.normal * plane.distance + v1 + v2,
            //    plane.normal * plane.distance - v1 + v2);
            //Gizmos.DrawLine(plane.normal * plane.distance - v1 - v2,
            //    plane.normal * plane.distance - v1 + v2);
            //Gizmos.DrawLine(plane.normal * plane.distance - v1 - v2,
            //    plane.normal * plane.distance + v1 - v2);
            
        }
    }
}
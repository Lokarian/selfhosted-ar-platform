using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnironmentMeshToggle : MonoBehaviour
{
    public Material VisibleMesh;
    public Material OcculusionMesh;
    public Material currentMaterial;

    private bool showMesh = true;
    

    public void ToggleMesh()
    {
        if (showMesh)
        {
            showMesh = false;
            currentMaterial = OcculusionMesh;
        }
        else
        {
            showMesh = true;
            currentMaterial = VisibleMesh;
        }
        foreach (var mesh in GameObject.FindObjectsOfType<EnvironmentMesh>())
        {
            mesh.GetComponent<MeshRenderer>().material = currentMaterial;
        }
    }
}

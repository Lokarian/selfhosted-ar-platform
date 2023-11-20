using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentMesh : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        GetComponent<MeshRenderer>().material = FindObjectOfType<EnironmentMeshToggle>().currentMaterial;
    }
}
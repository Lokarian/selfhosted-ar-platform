using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseDistanceHide : MonoBehaviour
{
    public float DistanceThreshold = 0.3f;
    private bool _isHidden = false;
    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(transform.position, Camera.main.transform.position) < DistanceThreshold)
        {
            if (!_isHidden)
            {
                _isHidden = true;
                foreach (var meshRenderer in GetComponentsInChildren<MeshRenderer>())
                {
                    meshRenderer.enabled = false;
                }
            }
        }
        else
        {
            if (_isHidden)
            {
                _isHidden = false;
                foreach (var meshRenderer in GetComponentsInChildren<MeshRenderer>())
                {
                    meshRenderer.enabled = true;
                }
            }
        }
    }
}

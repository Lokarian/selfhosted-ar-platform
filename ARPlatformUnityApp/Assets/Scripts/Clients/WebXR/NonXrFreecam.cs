using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class NonXrFreecam : MonoBehaviour
{
    FreeCam freecam;
    // Start is called before the first frame update
    void Start()
    {
        freecam = GetComponent<FreeCam>();
    }

    // Update is called once per frame
    void Update()
    {
        //check if currently in xr
        if (IsPresent())
        {
            //disable freecam
            freecam.enabled = false;
        }
        else
        {
            //enable freecam
            freecam.enabled = true;
        }
    }
    bool IsPresent()
    {
        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
        foreach (var xrDisplay in xrDisplaySubsystems)
        {
            if (xrDisplay.running)
            {
                return true;
            }
        }
        return false;
    }
}

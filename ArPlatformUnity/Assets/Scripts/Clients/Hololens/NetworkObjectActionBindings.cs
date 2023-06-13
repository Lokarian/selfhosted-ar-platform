using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObjectActionBindings : MonoBehaviour
{
    public void StartConnection()
    {
        GameObject.Find("ConnectionHandler").GetComponent<ClientConnectionHandler>().Connect();
    }
    
    public void ToggleEnvironmentMesh(bool active)
    {
        var environmentMeshHandler = GameObject.Find("EnvironmentMeshHandler");
        if (environmentMeshHandler)
        {
            environmentMeshHandler.GetComponent<EnvironmentMeshHandler>().StartTrackingEnvironment();
        }
        else
        {
            EnvironmentMeshHandler.allowStart = true;
        }
    }
}

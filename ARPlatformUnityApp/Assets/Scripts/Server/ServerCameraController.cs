using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ServerCameraController : MonoBehaviour
{
    public GameObject mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            //activate main camera
            mainCamera.SetActive(true);
        }
    }

}

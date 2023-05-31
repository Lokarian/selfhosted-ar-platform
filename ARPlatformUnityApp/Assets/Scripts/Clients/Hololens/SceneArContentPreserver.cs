using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneArContentPreserver : MonoBehaviour
{
    private bool foundUIRaycastCamera=false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!foundUIRaycastCamera)
        {
            GameObject uiRaycastCamera = GameObject.Find("UIRaycastCamera");
            if (uiRaycastCamera != null)
            {
                uiRaycastCamera.AddComponent<DontDestroyOnUnload>();
                foundUIRaycastCamera = true;
            }
        }
    }
}

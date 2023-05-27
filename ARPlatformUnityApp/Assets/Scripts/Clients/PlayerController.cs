using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(NetworkManager.Singleton.IsServer)
        {
            //
        }

        if (NetworkManager.Singleton.IsClient && IsOwner)
        {
            //activate camera of child gameobject
            transform.Find("Camera").gameObject.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

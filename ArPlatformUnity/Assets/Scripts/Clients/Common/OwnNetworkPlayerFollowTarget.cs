using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OwnNetworkPlayerFollowTarget : MonoBehaviour
{
    public Transform Target;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!Target)
        {
            if (NetworkManager.Singleton)
            {
                if (NetworkManager.Singleton.LocalClient?.PlayerObject)
                {
                    Target = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
                }
            }

        }
        else
        {
            var transform1 = transform;
            Target.position = transform1.position;
            Target.rotation = transform1.rotation;
        }
    }
}

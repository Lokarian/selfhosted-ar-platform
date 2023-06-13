using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OwnPlayerRendererHandler : NetworkBehaviour
{

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsLocalPlayer)
        {
            if (GetComponent<Renderer>())
            {
                GetComponent<Renderer>().enabled = false;
            }

            foreach (var childRenderer in GetComponentsInChildren<Renderer>())
            {
                childRenderer.enabled = false;
            }
        }
    }
}
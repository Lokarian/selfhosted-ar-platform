using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerFollower : MonoBehaviour
{
    public GameObject playerObject;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerObject)
        {
            //if we have a local player set playerObject to that
            if (NetworkManager.Singleton.LocalClient?.PlayerObject)
            {
                playerObject = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
                if (playerObject.GetComponent<Renderer>())
                {
                    playerObject.GetComponent<Renderer>().enabled = false;
                }
                foreach (var childRenderer in playerObject.GetComponentsInChildren<Renderer>())
                {
                    childRenderer.enabled = false;
                }
            }
        }
        else
        {
            var myTransform = transform;
            playerObject.transform.position = myTransform.position;
            playerObject.transform.rotation = myTransform.rotation;
        }
    }
}
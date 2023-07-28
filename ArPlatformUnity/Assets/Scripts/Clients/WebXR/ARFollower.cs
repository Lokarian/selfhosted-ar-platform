using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.GraphicsTools;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

public class ARFollower : MonoBehaviour
{
    public Button FollowButton;

    public Button StopFollowButton;

    private bool _isFollowing = false;
    public GameObject MyPlayer;
    public GameObject ARPlayer;

    private void Start()
    {
        if (GlobalConfig.Singleton.MyBuildTarget == ArBuildTarget.Web)
        {
            MyPlayer = Camera.main.gameObject;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Start is called before the first frame update
    public void StartFollowing()
    {
        //find object with hololens tag
        var arPlayer = GameObject.FindGameObjectWithTag("HololensPlayer");
        if (arPlayer == null)
        {
            return;
        }

        ARPlayer = arPlayer;
        MyPlayer.GetComponent<FlyCameraController>().enabled = false;
        _isFollowing = true;
        StopFollowButton.gameObject.SetActive(true);
        FollowButton.gameObject.SetActive(false);
    }

    public void StopFollowing()
    {
        MyPlayer.GetComponent<FlyCameraController>().enabled = true;
        _isFollowing = false;
        StopFollowButton.gameObject.SetActive(false);
        FollowButton.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (_isFollowing)
        {
            MyPlayer.transform.position = ARPlayer.transform.position;
            MyPlayer.transform.rotation = ARPlayer.transform.rotation;
        }
    }
}
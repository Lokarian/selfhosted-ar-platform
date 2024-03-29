using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class ClientConnectionHandler : MonoBehaviour
{
    void OnGUI()
    {
#if UNITY_EDITOR
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (GUILayout.Button("Connect")) Connect();

        GUILayout.EndArea();
#endif
    }

    public void Connect()
    {
#if UNITY_EDITOR
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WSAPlayer)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes("Hololens");
        }
        else if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes("WebXR");
        }
#else
        if (Application.platform == RuntimePlatform.WSAPlayerARM ||
                Application.platform == RuntimePlatform.WSAPlayerX64 ||
                Application.platform == RuntimePlatform.WSAPlayerX86)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes("Hololens");
        }
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes("WebXR");
        }
#endif
        NetworkManager.Singleton.StartClient();
    }
}

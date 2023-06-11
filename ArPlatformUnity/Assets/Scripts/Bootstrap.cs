using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    public bool DetectBuildTarget = true;

    private void Start()
    {
        if (DetectBuildTarget)
        {
#if UNITY_EDITOR
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WSAPlayer)
            {
                StartHololens();
                //StartWebXR();
            }
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            {
                StartWebXR();
            }
            else
            {
                StartServer();
            }
#else
            if (Application.platform == RuntimePlatform.WSAPlayerARM ||
                Application.platform == RuntimePlatform.WSAPlayerX64 ||
                Application.platform == RuntimePlatform.WSAPlayerX86)
            {
                StartHololens();
            }
            else if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                StartWebXR();
            }
            else
            {
                StartServer();
            }
#endif
        }
    }


    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Hololens")) StartHololens();
        if (GUILayout.Button("WebXR")) StartWebXR();
        if (GUILayout.Button("Server")) StartServer();
    }

    static void StatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
                        NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }

    void StartHololens()
    {
        //load scene scenes/Hololens/Connecting
        SceneManager.LoadScene("Scenes/Hololens/Connecting", LoadSceneMode.Single);
    }

    void StartWebXR()
    {
        SceneManager.LoadScene("Scenes/WebXR/Connecting", LoadSceneMode.Single);
    }

    void StartServer()
    {
        //SceneManager.LoadScene("Scenes/Server/Idle", LoadSceneMode.Single);
        SceneManager.LoadScene("Scenes/Server/Idle", LoadSceneMode.Single);
    }
}

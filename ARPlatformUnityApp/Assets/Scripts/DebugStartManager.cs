using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugStartManager: MonoBehaviour
{
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
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

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
        //todo
    }
    
    void StartServer()
    {
        //SceneManager.LoadScene("Scenes/Server/Idle", LoadSceneMode.Single);
        SceneManager.LoadScene("Scenes/Server/Idle", LoadSceneMode.Single);
    }
}
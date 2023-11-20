using System;
using Netcode.Transports.WebSocket;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.TLS;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerConfigurationManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        /*if (NetworkManager.Singleton.gameObject.GetComponent<WebSocketTransport>().SecureConnection)
        {
            NetworkManager.Singleton.gameObject.GetComponent<WebSocketTransport>().CertificateBase64String = GlobalConfig.Singleton.certificateBase64;
        }*/
        Debug.Log("ServerConfigurationManager Start");
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Starting Server");
            NetworkManager.Singleton.StartServer();
        }

        //DontDestroyOnLoad(gameObject);
        StartRemoteAssist();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (GUILayout.Button("Start Remote Assist")) StartRemoteAssist();

        GUILayout.EndArea();
    }

    void StartRemoteAssist()
    {
        //get ConnectionApprovalHandler and get ConnectionApprovalHandler script
        GameObject connectionApprovalHandlerObj = GameObject.Find("ConnectionApprovalHandler");
        var connectionApprovalHandler = connectionApprovalHandlerObj.GetComponent<ConnectionApprovalHandler>();
        connectionApprovalHandler.currentlyLocked = false;
        NetworkManager.Singleton.SceneManager.LoadScene("Scenes/Server/Remote Assist", LoadSceneMode.Single);
    }
}


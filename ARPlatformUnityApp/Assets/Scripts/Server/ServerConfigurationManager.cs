using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerConfigurationManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("ServerConfigurationManager Start");
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Starting Server");
            NetworkManager.Singleton.StartServer();
        }
        DontDestroyOnLoad(gameObject);
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
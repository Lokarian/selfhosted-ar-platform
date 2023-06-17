using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

public class GlobalConfig : MonoBehaviour
{
#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern string GetToken();
    [DllImport("__Internal")]
    private static extern string GetArSessionId();
    [DllImport("__Internal")]
    private static extern string GetServerUrl();
#endif


    public string AccessToken;
    public string ArSessionId;
    public string ServerUrl;
    public string certificateBase64;
    public string MyMemberId;
    public ArBuildTarget MyBuildTarget;


    private void Initialize()
    {
#if UNITY_EDITOR
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WSAPlayer)
        {
            ConfigureUWP(true);
        }
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
        {
            ConfigureWeb(true);
        }
        else
        {
            ConfigureServer(true);
        }
#else 
        if (Application.platform == RuntimePlatform.WSAPlayerARM ||
            Application.platform == RuntimePlatform.WSAPlayerX64 ||
            Application.platform == RuntimePlatform.WSAPlayerX86)
        {
            ConfigureUWP(false);
        }
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            ConfigureWeb(false);
        }
        else
        {
            ConfigureServer(false);
        }
#endif
        NetworkManager.Singleton.OnClientDisconnectCallback += (manager) =>
        {
            Debug.Log("Client disconnected from server");
        };
    }

    private void ConfigureServer(bool editor)
    {
        var privKey=File.ReadAllText("C:/ssl/privkey2.pem");
        var cert=File.ReadAllText("C:/ssl/fullchain2.pem");
        var unityTransport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();
        unityTransport.SetServerSecrets(cert,privKey);
        
        ServerUrl = "https://reithmeir.duckdns.org:5001";
        ArSessionId = "6a7a6c13-faf0-4668-89e4-ed98bcbc82f9";
        AccessToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjJmOGQxNDAyLTcwMTgtNDQ3ZS1iODcxLWFmNmJhZmZhODAyOCIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.jHWbwwR1sntNDRhPxwZULtXG4XMPPQZr3pS7JZYZbUU";
        MyBuildTarget = ArBuildTarget.Server;

    }

    private void ConfigureWeb(bool editor)
    {
        var unityTransport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();
        unityTransport.SetClientSecrets("reithmeir.duckdns.org");
        MyBuildTarget = ArBuildTarget.Web;
    }
    private void ConfigureUWP(bool editor)
    {
        var unityTransport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();
        unityTransport.SetClientSecrets("reithmeir.duckdns.org");

        if (editor)
        {
            
        }
        else
        {
            //get params from launch url
            var appUrl = Application.absoluteURL;
            var url = new Uri(appUrl);
            var query = url.Query;
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(query);
            ServerUrl = queryDictionary["serverUrl"];
            ArSessionId = queryDictionary["arSessionId"];
            MyBuildTarget = ArBuildTarget.Hololens;
            AccessToken = queryDictionary["token"];
        }
        MyBuildTarget = ArBuildTarget.Hololens;
    }
    
    private void Start()
    {
    
        if (Singleton == null)
        {
            Initialize();
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static GlobalConfig Singleton { get; private set; }
}

public enum ArBuildTarget
{
    Hololens,
    Web,
    Server
}
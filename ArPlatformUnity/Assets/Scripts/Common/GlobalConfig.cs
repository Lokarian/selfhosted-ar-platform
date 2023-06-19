using System;
using System.IO;
using System.Net;
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

    public bool EmulateRelease = false;

    public string AccessToken;
    public string ArSessionId;
    public string ServerUrl;
    public string certificateBase64;
    public string MyMemberId;
    public ArBuildTarget MyBuildTarget;
    


    private void Initialize()
    {
#if UNITY_EDITOR
        ArSessionId = "c3b66fb7-7322-46be-8c19-020b64aa89ea";
        ServerUrl = "https://localhost:5001";
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WSAPlayer)
        {
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjYwODdkYzFiLWRhZmEtNGUyNi04ODJkLWNjNzM0NTU5ODY4YSIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.9S0TOxxfZgtWeNuh7kmr2gtW1AzPJDvWAV-C2YnyBGg";
            ConfigureUWP(!EmulateRelease);
        }
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
        {
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjYwODdkYzFiLWRhZmEtNGUyNi04ODJkLWNjNzM0NTU5ODY4YSIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.9S0TOxxfZgtWeNuh7kmr2gtW1AzPJDvWAV-C2YnyBGg";
            ConfigureWeb(!EmulateRelease);
        }
        else
        {
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjViOTQ3MTczLTQ3ZTEtNDVhMy1hZDQxLWVmZmQ0MWRmNzVjOCIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.TRPN3jTNlYSY76Iquc3-TDMcb_7VkyPovHD9VaKCcGg";
            ConfigureServer(!EmulateRelease);
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
        MyBuildTarget = ArBuildTarget.Server;
        if (!editor)
        {
            ServerUrl = "https://reithmeir.duckdns.org:5001";
            ArSessionId = "c3b66fb7-7322-46be-8c19-020b64aa89ea";
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjY1NjE1ZDRiLTQ0OGYtNGNlMy04MWE4LTNmMWM3NzdjNzllNyIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.X2-lhesCPEuLhdmO6ZBosdEQe8-eUFddXsVFe6uhgcY";
        }
    }

    private void ConfigureWeb(bool editor)
    {
        MyBuildTarget = ArBuildTarget.Web;
        if (!editor)
        {
            ServerUrl = "https://reithmeir.duckdns.org:5001";
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjY3YmM2NDRkLTIwNDktNGQ0NS05YWUwLWYwMGYzOTMyODcyZSIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.MZ3oi2wKz7dBwPR8LORbGxfr-PyUu2frnuYeZntdG2k";
            ArSessionId = "c3b66fb7-7322-46be-8c19-020b64aa89ea";
        }
    }

    private void ConfigureUWP(bool editor)
    {
        

        if (editor)
        {
        }
        else
        {
            //get params from launch url
            /*var appUrl = Application.absoluteURL;
            var url = new Uri(appUrl);
            var query = url.Query;
            var queryDictionary = WebUtility.
            ServerUrl = queryDictionary["serverUrl"];
            ArSessionId = queryDictionary["arSessionId"];
            MyBuildTarget = ArBuildTarget.Hololens;
            AccessToken = queryDictionary["token"];*/
            ServerUrl = "https://reithmeir.duckdns.org:5001";
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjY3YmM2NDRkLTIwNDktNGQ0NS05YWUwLWYwMGYzOTMyODcyZSIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.MZ3oi2wKz7dBwPR8LORbGxfr-PyUu2frnuYeZntdG2k";
            ArSessionId = "c3b66fb7-7322-46be-8c19-020b64aa89ea";
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
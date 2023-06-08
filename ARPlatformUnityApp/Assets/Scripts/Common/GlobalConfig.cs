using System;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
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
        ServerUrl = "https://localhost:5001";
        ArSessionId = "c3b66fb7-7322-46be-8c19-020b64aa89ea";
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WSAPlayer)
        {
            MyBuildTarget = ArBuildTarget.Hololens;
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjYwODdkYzFiLWRhZmEtNGUyNi04ODJkLWNjNzM0NTU5ODY4YSIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.9S0TOxxfZgtWeNuh7kmr2gtW1AzPJDvWAV-C2YnyBGg";
        }
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
        {
            MyBuildTarget = ArBuildTarget.Web;
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjYwODdkYzFiLWRhZmEtNGUyNi04ODJkLWNjNzM0NTU5ODY4YSIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.9S0TOxxfZgtWeNuh7kmr2gtW1AzPJDvWAV-C2YnyBGg";
        }
        else
        {
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjI2MjllZThkLTA1NGEtNDE5OS1hN2QzLTNjZWIyNzkwODZiYyIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.CEOlS11isfLxeT66KaEbpgBsuFr64rjNMor-fPdr1Ls";
            MyBuildTarget = ArBuildTarget.Server;
            try
            {
                var certificate = new X509Certificate2("C:/ssl/cert.pem").Export(X509ContentType.Cert);
                certificateBase64 = Convert.ToBase64String(certificate);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
#elif UNITY_WEBGL
        ServerUrl = GetServerUrl();
        ArSessionId = GetArSessionId();
        MyBuildTarget = ArBuildTarget.Web;
        AccessToken = GetToken();

#else
#endif

        if (Application.platform == RuntimePlatform.WSAPlayerARM ||
            Application.platform == RuntimePlatform.WSAPlayerX64 ||
            Application.platform == RuntimePlatform.WSAPlayerX86)
        {
            if (Application.absoluteURL != null&&Application.absoluteURL.Length>0)
            {
                var appUrl = Application.absoluteURL;
                var url = new Uri(appUrl);
                var query = url.Query;
                var queryDictionary = System.Web.HttpUtility.ParseQueryString(query);
                ServerUrl = queryDictionary["serverUrl"];
                ArSessionId = queryDictionary["arSessionId"];
                MyBuildTarget = ArBuildTarget.Hololens;
                AccessToken = queryDictionary["token"];
            }
            else
            {
                ServerUrl="https://reithmeir.duckdns.org:5001";
                ArSessionId = "6a7a6c13-faf0-4668-89e4-ed98bcbc82f9";
                AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjdlOWY1NzdhLTM2MDMtNDFiZi1iNjYxLWMxMmI0MWNhMTRiYyIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.wAVRRavTj-bjkPRU9JRxqGK8nIWS76OYvWeufKwA3ZI";
                MyBuildTarget = ArBuildTarget.Hololens;
            }
            
        }
        else
        {
            ServerUrl = "https://reithmeir.duckdns.org:5001";
            ArSessionId = "6a7a6c13-faf0-4668-89e4-ed98bcbc82f9";
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjJmOGQxNDAyLTcwMTgtNDQ3ZS1iODcxLWFmNmJhZmZhODAyOCIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.jHWbwwR1sntNDRhPxwZULtXG4XMPPQZr3pS7JZYZbUU";
            try
            {
                var certificate = new X509Certificate2("C:/ssl/cert.pem").Export(X509ContentType.Cert);
                certificateBase64 = Convert.ToBase64String(certificate);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
//#endif
    }

    private void Awake()
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
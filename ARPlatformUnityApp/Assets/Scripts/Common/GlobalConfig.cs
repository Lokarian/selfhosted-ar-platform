using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Netcode.Transports.WebSocket;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;


public class GlobalConfig : MonoBehaviour
{
    public string JwtToken;
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
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WSAPlayer )
        {
            MyBuildTarget = ArBuildTarget.Hololens;
            JwtToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjYwODdkYzFiLWRhZmEtNGUyNi04ODJkLWNjNzM0NTU5ODY4YSIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.9S0TOxxfZgtWeNuh7kmr2gtW1AzPJDvWAV-C2YnyBGg";
        }
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
        {
            MyBuildTarget = ArBuildTarget.Web;
            JwtToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjYwODdkYzFiLWRhZmEtNGUyNi04ODJkLWNjNzM0NTU5ODY4YSIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.9S0TOxxfZgtWeNuh7kmr2gtW1AzPJDvWAV-C2YnyBGg";
        }
        else
        {
            JwtToken =
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
        #else
        ServerUrl = "https://reithmeir.duckdns.org:5001";
        ArSessionId = "c3b66fb7-7322-46be-8c19-020b64aa89ea";
        if (Application.platform == RuntimePlatform.WSAPlayerARM ||
                Application.platform == RuntimePlatform.WSAPlayerX64 ||
                Application.platform == RuntimePlatform.WSAPlayerX86 ||
                Application.platform == RuntimePlatform.WebGLPlayer)
        {
            JwtToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjYwODdkYzFiLWRhZmEtNGUyNi04ODJkLWNjNzM0NTU5ODY4YSIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.9S0TOxxfZgtWeNuh7kmr2gtW1AzPJDvWAV-C2YnyBGg";
        }
        else
        {
            JwtToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjI2MjllZThkLTA1NGEtNDE5OS1hN2QzLTNjZWIyNzkwODZiYyIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMS8iLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjUwMDEvIn0.CEOlS11isfLxeT66KaEbpgBsuFr64rjNMor-fPdr1Ls";
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
        #endif

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

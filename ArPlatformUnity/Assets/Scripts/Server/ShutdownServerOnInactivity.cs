using System;
using AsciiFBXExporter;
using Unity.Netcode;
using UnityEngine;

namespace Server
{
    public class ShutdownServerOnInactivity:MonoBehaviour
    {
        public float InactivityTimeout = 60f;
        public float NoConnectionStartTime=-1f;
        public bool SaveMeshesOnShutdown = true;
        public string MeshSavePath;
        private bool _shutdownInitiated = false;
        void OnGUI()
        {

            if(GUI.Button(new Rect(150, 100, 150, 50), "shutdown process"))
            {
                this.InitiateShutdown();
            }

        }
        private void Start()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_shutdownInitiated)
            {
                return;
            }
            if(NetworkManager.Singleton.ConnectedClients.Count==0)
            {
                if (NoConnectionStartTime < 0)
                {
                    NoConnectionStartTime = Time.realtimeSinceStartup;
                }
                else if (Time.realtimeSinceStartup - NoConnectionStartTime > InactivityTimeout)
                {
                    InitiateShutdown();
                    
                }
            }
            else
            {
                NoConnectionStartTime = -1f;
            }
        }

        public void InitiateShutdown()
        {
            _shutdownInitiated = true;
            var exporter=GetComponent<RuntimeExporterMono>();
            Debug.Log("Initiating shutdown");
            exporter.AbsolutePath += GlobalConfig.Singleton.ArSessionId;
            var meshHandler = FindObjectOfType<EnvironmentMeshHandler>();
            exporter.rootObjectToExport = meshHandler.gameObject;
            Debug.Log($"Exporting to {exporter.AbsolutePath}");
            exporter.ExportGameObject();
            //after 30 seconds, shutdown the server
            Invoke(nameof(ShutdownServer), 10f);

        }
        public void ShutdownServer()
        {
            Application.Quit();
        }
    }
}
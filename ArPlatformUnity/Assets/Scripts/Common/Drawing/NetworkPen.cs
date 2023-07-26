using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkPen : NetworkBehaviour
{
    public GameObject LinePrefab;
    public Transform LineParentGameObject;
    
    private bool _isDrawing = false;
    
    public Renderer Renderer;
    public Color Color=Color.green;
    public Color InactiveColor=Color.gray;

    private Dictionary<ulong, List<NetworkLine>> _userLines = new();

    public void StartDrawing()
    {
        if (_isDrawing)
        {
            return;
        }
        _isDrawing = true;
        var lineGuid = Guid.NewGuid().ToString();
        CreateLine_ServerRpc(lineGuid);
        Renderer.material.color = Color;
    }
    

    private void Update()
    {
        if (_isDrawing)
        {
            Debug.Log($"AddPoint_ServerRpc {transform.position}");
            AddPoint_ServerRpc(transform.position);
            
        }
    }

    public void StopDrawing()
    {
        _isDrawing = false;
        Renderer.material.color = InactiveColor;
    }

    public void Hide()
    {
        Renderer.enabled = false;
    }

    public void Show()
    {
        Renderer.enabled = true;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void CreateLine_ServerRpc(string guid, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("CreateLine_ServerRpc");
        var go = Instantiate(LinePrefab,LineParentGameObject);
        go.GetComponent<NetworkObject>().Spawn();
        go.name= "NetworkLine_"+guid;
        if(!_userLines.TryGetValue(serverRpcParams.Receive.SenderClientId,out var lines))
        {
            lines = new List<NetworkLine>();
            _userLines.Add(serverRpcParams.Receive.SenderClientId,lines);
        }
        lines.Add(go.GetComponent<NetworkLine>());
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void AddPoint_ServerRpc(Vector3 point, ServerRpcParams serverRpcParams = default)
    {
        var networkLine = _userLines[serverRpcParams.Receive.SenderClientId].LastOrDefault();
        if (!networkLine)
        {
            Debug.LogError("NetworkLine not found");
            return;
        }
        networkLine.AddPoint(point);
    }
    
}

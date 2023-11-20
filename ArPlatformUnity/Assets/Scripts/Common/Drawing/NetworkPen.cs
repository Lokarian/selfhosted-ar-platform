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
        var width = Vector3.Distance(Camera.main.transform.position, transform.position) * 0.01f;
        CreateLine_ServerRpc(lineGuid, Color, width);
        Renderer.material.color = Color;
    }
    public void SetSize(float size)
    {
        transform.localScale = Vector3.one * size;
    }

    private void Update()
    {
        if (_isDrawing)
        {
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
    public void CreateLine_ServerRpc(string guid,Color color,float width, ServerRpcParams serverRpcParams = default)
    {
        var go = Instantiate(LinePrefab,LineParentGameObject);
        go.GetComponent<NetworkObject>().Spawn();
        go.name= "NetworkLine_"+guid;
        go.GetComponent<NetworkLine>().Color.Value=color;
        go.GetComponent<NetworkLine>().Width.Value=width;
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

    [ServerRpc(RequireOwnership = false)] 
    public void DeleteMyLastLine_ServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var networkLine = _userLines[serverRpcParams.Receive.SenderClientId].LastOrDefault();
        if (!networkLine)
        {
            return;
        }
        Destroy(networkLine.gameObject);
        _userLines[serverRpcParams.Receive.SenderClientId].Remove(networkLine);
    }
}

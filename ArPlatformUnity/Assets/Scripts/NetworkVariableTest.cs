using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkVariableTest : NetworkBehaviour
{
    private NetworkVariable<NativeList<byte>> networkVar = new NetworkVariable<NativeList<byte>>(new NativeList<byte>(Allocator.Persistent),
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    //slider in editor
    [Range(0, 50000)]
    public int DataSize = 1000;
    
    [Range(0, 30)]
    public int FramePause = 0;
    
    
    [DebugGUIGraph()]
    public float MBytePerSecond;

    [DebugGUIGraph(group:1)]
    public float FPS = 0;
    
    
    private List<Tuple<float,int>> _bytesReceivedWithTimestamp = new List<Tuple<float, int>>();

    private int _framePauseCounter = 0;

    private bool go = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnGUI()
    {
        //two sliders top left corner that do the same as the inspector sliders
        DataSize = (int)GUI.HorizontalSlider(new Rect(10, 10, 100, 20), DataSize, 0, 50000);
        FramePause = (int)GUI.HorizontalSlider(new Rect(10, 30, 100, 20), FramePause, 0, 30);
    }

    // Update is called once per frame
    void Update()
    {
        if(!go)
        {
            return;
        }
        if(_framePauseCounter < FramePause)
        {
            _framePauseCounter++;
            return;
        }
        _framePauseCounter = 0;
        var data = new List<byte>();
        for (int i = 0; i < DataSize; i++)
        {
            data.Add((byte)Random.Range(0, 255));
        }
        networkVar.Value.Clear();
        var native=data.ToNativeArray(Allocator.Temp);
        networkVar.Value.AddRange(native);
        networkVar.SetDirty(true);
        native.Dispose();
        
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestOwnership_ServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("RequestOwnership_ServerRpc from " + rpcParams.Receive.SenderClientId);
        GetComponent<NetworkObject>().ChangeOwnership(rpcParams.Receive.SenderClientId);
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (IsServer)
        {
            return;
        }
        Debug.Log("OnGainedOwnership");
        go = true;
    }

    public override void OnNetworkSpawn()
    {
        networkVar.OnValueChanged += (old, newValue) =>
        {
            Debug.Log($"OnValueChanged: {newValue.Length} {newValue[0]}");
            LogDataReceived(newValue.Length);
        };
        if (!IsServer)
        {
            Debug.Log("Requesting ownership");
            RequestOwnership_ServerRpc();
        }
    }

    private void LogDataReceived(int amountBytes)
    {
        _bytesReceivedWithTimestamp.Add(new Tuple<float, int>(Time.realtimeSinceStartup, amountBytes));
        //filter all data older than 1 second
        _bytesReceivedWithTimestamp.RemoveAll(tuple => tuple.Item1 < Time.realtimeSinceStartup - 1);
        //calculate the amount of bytes received in the last second
        var bytesReceivedInLastSecond = 0;
        foreach (var tuple in _bytesReceivedWithTimestamp)
        {
            bytesReceivedInLastSecond += tuple.Item2;
        }
        //convert to MByte
        MBytePerSecond = bytesReceivedInLastSecond / 1024f / 1024f;
        
        FPS=_bytesReceivedWithTimestamp.Count;
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkTexture : NetworkBehaviour
{
    private Texture2D _texture;

    //public getter
    public Texture2D Texture
    {
        get => _texture;
        private set => _texture = value;
    }

    public NetworkMesh NetworkMesh;

    private Dictionary<ulong, Coroutine> _sendTextureCoroutines = new();

    private List<byte> _receivedBytes = new List<byte>();

    public NetworkVariable<int> TextureSize = new NetworkVariable<int>(2048, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);


    public void SetTexture(Texture2D texture, int? version = null)
    {
        Texture = texture;
        GetComponent<MeshRenderer>().material.mainTexture = Texture;
        if (IsServer && IsOwner)
        {
            TextureSize.Value = texture.width;
            foreach (var sendTextureCoroutine in _sendTextureCoroutines)
            {
                if (sendTextureCoroutine.Value != null)
                {
                    StopCoroutine(sendTextureCoroutine.Value);
                }
            }

            _sendTextureCoroutines.Clear();
            HashSet<ulong>.Enumerator visibleClients = this.NetworkObject.GetObservers();
            while (visibleClients.MoveNext())
            {
                _sendTextureCoroutines.Add(visibleClients.Current,
                    StartCoroutine(SendTextureCoroutine(texture, visibleClients.Current)));
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn");
        base.OnNetworkSpawn();
        //request initial mesh from server if we are not the server and not the owner
        if (!IsServer && !IsOwner)
        {
            Debug.Log("Requesting initial texture from server");
            RequestInitialTexture_ServerRpc();
        }
    }
    
    public IEnumerator SendTextureCoroutine(Texture2D texture, ulong clientId)
    {
        Debug.Log("SendTextureCoroutine to client " + clientId);
        var bytesLeft = ToBytes(texture);

        var chunkNumber = 0;
        while (bytesLeft.Count > 0)
        {
            //check if we can allocate bytes to client, prevent overflow of send buffer
            if (BandwidthAllocator.Singleton.TryAllocateBytesToClient(clientId, bytesLeft.Count, out var actualBytes))
            {
                var bytesToSend = bytesLeft.Take(actualBytes).ToList();
                bytesLeft.RemoveRange(0, bytesToSend.Count);
                bytesLeft.TrimExcess();

                UpdateTextureChunk_ClientRpc(new ByteListWrapper(bytesToSend), chunkNumber, bytesLeft.Count == 0,
                    new ClientRpcParams()
                        { Send = new ClientRpcSendParams() { TargetClientIds = new[] { clientId } } });


                chunkNumber++;
            }

            yield return null;
        }

        _sendTextureCoroutines.Remove(clientId);
    }

    [ClientRpc]
    public void UpdateTextureChunk_ClientRpc(ByteListWrapper bytes, int chunkNumber, bool isLastChunk,
        ClientRpcParams clientRpcParams = default)
    {
        if (chunkNumber == 0)
        {
            _receivedBytes.Clear();
        }

        _receivedBytes.AddRange(bytes.Bytes);
        if (isLastChunk)
        {
            Texture = FromBytes(_receivedBytes, TextureSize.Value);
            GetComponent<MeshRenderer>().material.mainTexture = Texture;
            _receivedBytes.Clear();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void RequestInitialTexture_ServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (Texture != null && !_sendTextureCoroutines.ContainsKey(serverRpcParams.Receive.SenderClientId))
        {
            _sendTextureCoroutines.Add(serverRpcParams.Receive.SenderClientId,
                StartCoroutine(SendTextureCoroutine(Texture, serverRpcParams.Receive.SenderClientId)));
        }
    }
    public List<byte> ToBytes(Texture2D texture)
    {
        return texture.EncodeToPNG().ToList();
    }

    public Texture2D FromBytes(List<byte> bytes, int size)
    {
        var texture = new Texture2D(size, size);
        texture.LoadImage(bytes.ToArray());
        texture.Apply();
        return texture;
    }
}

public class ByteListWrapper : INetworkSerializable
{
    public byte[] Bytes;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Bytes);
    }

    public ByteListWrapper(List<byte> bytes)
    {
        Bytes = bytes.ToArray();
    }

    public ByteListWrapper(byte[] bytes)
    {
        Bytes = bytes;
    }
    
    public ByteListWrapper()
    {
    }
}
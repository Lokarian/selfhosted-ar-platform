using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class NetworkTexture : NetworkBehaviour
{
    private Texture2D _texture;

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

    private List<byte> _pngRepresentation = new List<byte>();

    Tuple<int, Texture2D> _waitingTexture;
    public int? CurrentVersion;


    public void SetTexture(Texture2D texture, int? version = null)
    {
        if (version != null)
        {
            if (NetworkMesh != null && !NetworkMesh.SynchroniseVersions(version.Value))
            {
                _waitingTexture = new Tuple<int, Texture2D>(version.Value, texture);
                return;
            }
            CurrentVersion = version;
        }

        if (Texture)
        {
            Destroy(Texture);
        }

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

    public void SetTextureWithReadback(Texture2D texture, int? version = null)
    {
        NativeArray<byte> byteArray =
            new NativeArray<byte>(texture.height * texture.width * 4, Allocator.Persistent);
        AsyncGPUReadback.RequestIntoNativeArray(ref byteArray, texture, 0, GraphicsFormat.R8G8B8A8_SRGB,
            (request) =>
            {
                if (request.hasError)
                {
                    Debug.LogError("GPU readback error detected.");
                    byteArray.Dispose();
                    return;
                }

                var imageBytes = ImageConversion.EncodeNativeArrayToPNG(byteArray, GraphicsFormat.R8G8B8A8_SRGB,
                    (uint)request.width, (uint)request.height);
                _pngRepresentation = imageBytes.ToList();
                imageBytes.Dispose();
                byteArray.Dispose();
                SetTexture(texture, version);
            });
    }

    public bool SynchroniseVersions(int version)
    {
        if (_waitingTexture == null)
        {
            return false;
        }

        if (version == _waitingTexture.Item1)
        {
            CurrentVersion = version;
            var waitingTexture = _waitingTexture;
            _waitingTexture = null;
            SetTexture(waitingTexture.Item2);
            return true;
        }

        return false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //request initial mesh from server if we are not the server and not the owner
        if (!IsServer && !IsOwner)
        {
            RequestInitialTexture_ServerRpc();
        }
    }


    public IEnumerator SendTextureCoroutine(Texture2D texture, ulong clientId)
    {
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
                    CurrentVersion ?? -1,
                    new ClientRpcParams()
                        { Send = new ClientRpcSendParams() { TargetClientIds = new[] { clientId } } });


                chunkNumber++;
            }

            yield return null;
        }

        _sendTextureCoroutines.Remove(clientId);
    }

    [ClientRpc]
    public void UpdateTextureChunk_ClientRpc(ByteListWrapper bytes, int chunkNumber, bool isLastChunk, int version,
        ClientRpcParams clientRpcParams = default)
    {
        if (chunkNumber == 0)
        {
            _receivedBytes.Clear();
        }

        _receivedBytes.AddRange(bytes.Bytes);
        if (isLastChunk)
        {
            var texture = FromBytes(_receivedBytes, TextureSize.Value);
            SetTexture(texture, version == -1 ? null : version);
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
        //var bytes = texture.EncodeToPNG();
        return _pngRepresentation.ToList();
    }

    public Texture2D FromBytes(List<byte> bytes, int size)
    {
        var texture = new Texture2D(size, size);
        texture.LoadImage(bytes.ToArray());
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
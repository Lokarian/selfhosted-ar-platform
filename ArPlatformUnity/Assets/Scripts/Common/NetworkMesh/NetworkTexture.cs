using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NetworkTexture : MonoBehaviour
{
    private Texture2D _texture;
    //public getter
    public Texture2D Texture
    {
        get => _texture;
        private set => _texture = value;
    }
    public NetworkMesh NetworkMesh;

    public NetworkVariable<int> TextureSize = new NetworkVariable<int>(2048, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);


    public void SetTexture(Texture2D texture,int? version=null)
    {
        
        var bytes = ToBytes(texture);
        Debug.Log("Texture if size " +bytes.Count);
        string randomName = System.Guid.NewGuid().ToString();
        System.IO.File.WriteAllBytes($"C:/temp/arplatform/{randomName}.png",bytes.ToArray());
        Texture = FromBytes(bytes,texture.width);
        GetComponent<MeshRenderer>().material.mainTexture = Texture;
    }

    /*public IEnumerator SendTextureCoroutine(Texture2D texture, ulong clientId)
    {
        Debug.Log("SendTextureCoroutine to client " + clientId);
        var bytesLeft = ToJpeg(texture);
        
        var chunkNumber = 0;
        while (bytesLeft.Count > 0)
        {
            //check if we can allocate bytes to client, prevent overflow of send buffer
            if (BandwidthAllocator.Singleton.TryAllocateBytesToClient(clientId, bytesLeft.Count, out var actualBytes))
            {
                var bytesToSend = bytesLeft.Take(actualBytes).ToList();
                bytesLeft.RemoveRange(0, bytesToSend.Count);
                bytesLeft.TrimExcess();

                if (clientId == 0)
                {
                    UpdateTextureChunk_ServerRpc(bytesToSend, chunkNumber, bytesLeft == 0);
                }
                else
                {
                    UpdateMeshChunk_ClientRpc(verticesToSend, trianglesToSend,uvsToSend, chunkNumber, bytesLeft == 0,
                        new ClientRpcParams()
                            { Send = new ClientRpcSendParams() { TargetClientIds = new[] { clientId } } });
                }

                chunkNumber++;
            }

            yield return null;
        }
        _sendMeshCoroutines.Remove(clientId);
    }*/

    public List<byte> ToBytes(Texture2D texture)
    {
        return texture.EncodeToPNG().ToList();
    }

    public Texture2D FromBytes(List<byte> bytes,int size)
    {
        var texture = new Texture2D(size, size);
        texture.LoadImage(bytes.ToArray());
        texture.Apply();
        return texture;
    }
    
    
    
}

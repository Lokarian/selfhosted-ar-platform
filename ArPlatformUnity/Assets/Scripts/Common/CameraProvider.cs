using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
#if !UNITY_WEBGL
using UnityEngine.Windows.WebCam;
using WebSocketSharp.Server;
#endif

public class CameraProvider : NetworkBehaviour
{
    public GameObject PositionedPhotoPrefab;

    private List<byte> _imageBufferList = new List<byte>();
    private Coroutine _sendPhotoCoroutine;

#if !UNITY_WEBGL
    private PhotoCapture photoCaptureObject = null;
#endif
    private bool alreadyTakingPhoto = false;

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        if (GlobalConfig.Singleton.MyBuildTarget != ArBuildTarget.Hololens || !GlobalConfig.Singleton.ShowEnvironment)
        {
            return;
        }

#if !UNITY_WEBGL
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
#endif
    }

    [ClientRpc]
    public void HololensTakePhoto_ClientRpc(ClientRpcParams param = default)
    {
        if (GlobalConfig.Singleton.MyBuildTarget != ArBuildTarget.Hololens || !GlobalConfig.Singleton.ShowEnvironment)
        {
            return;
        }

        if (alreadyTakingPhoto)
        {
            return;
        }
        alreadyTakingPhoto = true;
        PauseBrowserCamera();
        TryTakePhoto();
    }

    public void TryTakePhoto()
    {
#if !UNITY_WEBGL
        Resolution cameraResolution =
 PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.JPEG;
        Debug.Log("Camera Resolution: " + c.cameraResolutionWidth + "x" + c.cameraResolutionHeight);
        photoCaptureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
#endif
    }

#if !UNITY_WEBGL
    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;
    }
    
    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            Debug.Log("Photo mode started!");
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
            Debug.LogError("Unable to start photo mode, retrying...");
            Invoke(nameof(TryTakePhoto), 0.1f);
        }
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            List<byte> imageBufferList = new List<byte>();
            // Copy the raw IMFMediaBuffer data into our empty byte list.
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);
            if (!photoCaptureFrame.TryGetProjectionMatrix(0.1f, 50f, out Matrix4x4 matrix))
            {
                Debug.LogError("Unable to get projection matrix!");
            }

            if (!photoCaptureFrame.TryGetCameraToWorldMatrix(out Matrix4x4 cameraToWorldMatrix))
            {
                Debug.LogError("Unable to get camera to world matrix!");
            }

            Debug.Log("Image Buffer List Count: " + imageBufferList.Count);
            SendToServer(matrix, cameraToWorldMatrix,
                imageBufferList);
        }
        else
        {
            Debug.LogError("Failed to capture photo to memory!");
        }

        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        ResumeBrowserCamera();
        alreadyTakingPhoto = false;
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
    }
#endif
    [ServerRpc(RequireOwnership = false)]
    public void TakePhoto_ServerRpc(ServerRpcParams param = default)
    {
        Debug.Log("Take Photo Server RPC");
        HololensTakePhoto_ClientRpc();
    }

    public void ButtonTakePhoto()
    {
        TakePhoto_ServerRpc();
    }

    private void SendToServer(Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix, List<byte> imageBufferList)
    {
        if (_sendPhotoCoroutine != null)
        {
            Debug.LogWarning("Did not finish sending previous photo!");
            return;
        }

        _sendPhotoCoroutine =
            StartCoroutine(SendPhotoCoroutine(projectionMatrix, cameraToWorldMatrix, imageBufferList));
    }

    public IEnumerator SendPhotoCoroutine(Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix,
        List<byte> imageBufferList)
    {
        var bytesLeft = imageBufferList.ToList();
        var chunkNumber = 0;
        while (bytesLeft.Count > 0)
        {
            if (BandwidthAllocator.Singleton.TryAllocateBytesToClient(0, bytesLeft.Count, out var actualBytes))
            {
                var bytesToSend = bytesLeft.Take(actualBytes).ToArray();
                bytesLeft.RemoveRange(0, actualBytes);
                var lastChunk = bytesLeft.Count == 0;
                var projectionMatrixAsArray = Array.Empty<float>();
                var cameraToWorldMatrixAsArray = Array.Empty<float>();
                if (lastChunk)
                {
                    // Send the projection matrix and camera to world matrix with the last chunk
                    projectionMatrixAsArray = new float[16];
                    cameraToWorldMatrixAsArray = new float[16];
                    for (var i = 0; i < 16; i++)
                    {
                        projectionMatrixAsArray[i] = projectionMatrix[i];
                        cameraToWorldMatrixAsArray[i] = cameraToWorldMatrix[i];
                    }
                }

                SendImageChunk_ServerRpc(bytesToSend, chunkNumber, lastChunk, projectionMatrixAsArray,
                    cameraToWorldMatrixAsArray);
                chunkNumber++;
            }

            yield return null;
        }

        _sendPhotoCoroutine = null;
    }


    [ServerRpc(RequireOwnership = false)]
    void SendImageChunk_ServerRpc(byte[] bytes, int chunkNumber, bool lastChunk, float[] projectionMatrixAsArray,
        float[] cameraToWorldMatrixAsArray)
    {
        if (!lastChunk)
        {
            if (chunkNumber == 0)
                _imageBufferList.Clear();
            _imageBufferList.AddRange(bytes);
            return;
        }

        _imageBufferList.AddRange(bytes);

        var projectionMatrix = new Matrix4x4();
        projectionMatrix.SetColumn(0,
            new Vector4(projectionMatrixAsArray[0], projectionMatrixAsArray[1], projectionMatrixAsArray[2],
                projectionMatrixAsArray[3]));
        projectionMatrix.SetColumn(1,
            new Vector4(projectionMatrixAsArray[4], projectionMatrixAsArray[5], projectionMatrixAsArray[6],
                projectionMatrixAsArray[7]));
        projectionMatrix.SetColumn(2,
            new Vector4(projectionMatrixAsArray[8], projectionMatrixAsArray[9], projectionMatrixAsArray[10],
                projectionMatrixAsArray[11]));
        projectionMatrix.SetColumn(3,
            new Vector4(projectionMatrixAsArray[12], projectionMatrixAsArray[13], projectionMatrixAsArray[14],
                projectionMatrixAsArray[15]));

        var cameraToWorldMatrix = new Matrix4x4();
        cameraToWorldMatrix.SetColumn(0,
            new Vector4(cameraToWorldMatrixAsArray[0], cameraToWorldMatrixAsArray[1], cameraToWorldMatrixAsArray[2],
                cameraToWorldMatrixAsArray[3]));
        cameraToWorldMatrix.SetColumn(1,
            new Vector4(cameraToWorldMatrixAsArray[4], cameraToWorldMatrixAsArray[5], cameraToWorldMatrixAsArray[6],
                cameraToWorldMatrixAsArray[7]));
        cameraToWorldMatrix.SetColumn(2,
            new Vector4(cameraToWorldMatrixAsArray[8], cameraToWorldMatrixAsArray[9], cameraToWorldMatrixAsArray[10],
                cameraToWorldMatrixAsArray[11]));
        cameraToWorldMatrix.SetColumn(3,
            new Vector4(cameraToWorldMatrixAsArray[12], cameraToWorldMatrixAsArray[13], cameraToWorldMatrixAsArray[14],
                cameraToWorldMatrixAsArray[15]));


        CreatePositionedPhoto(projectionMatrix, cameraToWorldMatrix, _imageBufferList.ToArray());
    }

    public void CreatePositionedPhoto(Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix,
        byte[] imageBufferList)
    {
        //load imageBufferList as IMFMediaBuffer into texture
        var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        texture.LoadImage(imageBufferList);
        texture.Apply();
        var positionedPhoto = Instantiate(PositionedPhotoPrefab);
        positionedPhoto.transform.SetParent(transform, true);
        positionedPhoto.GetComponent<PositionedPhoto>()
            .Initialize(projectionMatrix, cameraToWorldMatrix, texture.width, texture.height, texture);
    }


    private void PauseBrowserCamera()
    {
        GetComponent<WebSocketServer.WebSocketServer>().SendMessageToClient("pause");
    }

    private void ResumeBrowserCamera()
    {
        GetComponent<WebSocketServer.WebSocketServer>().SendMessageToClient("resume");
    }
}
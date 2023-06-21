using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Windows.WebCam;

public class CameraProvider : NetworkBehaviour
{
    public GameObject PositionedPhotoPrefab;
    private PhotoCapture photoCaptureObject = null;
    private Resolution cameraResolution;
    private List<byte> _imageBufferList = new List<byte>();
    public bool TakePhoto = false;
    private Coroutine _sendPhotoCoroutine;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (TakePhoto)
        {
            TakePhoto = false;
            StartTakingPhoto();
        }
    }

    [Button]
    public void StartTakingPhoto()
    {
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }

    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;

        cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        Debug.Log("Camera Resolution: " + cameraResolution.width + "x" + cameraResolution.height);
        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.PNG;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
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
            SendToServer(matrix, cameraToWorldMatrix, cameraResolution.width, cameraResolution.height,
                imageBufferList.ToArray());
        }

        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    private void SendToServer(Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix, int width, int height,
        byte[] imageBufferList)
    {
        if (_sendPhotoCoroutine != null )
        {
            Debug.LogWarning("Did not finish sending previous photo!");
            return;
        }
        _sendPhotoCoroutine = StartCoroutine(SendPhotoCoroutine(projectionMatrix, cameraToWorldMatrix, width, height,
            imageBufferList));
    }
    public IEnumerator SendPhotoCoroutine(Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix, int width, int height,
        byte[] imageBufferList)
    {
        var bytesLeft = imageBufferList.ToList();
        var chunkNumber = 0;
        while (bytesLeft.Count > 0)
        {
            if (BandwidthAllocator.Singleton.TryAllocateBytesToClient(0, bytesLeft.Count, out var actualBytes))
            {
                var bytesToSend= bytesLeft.Take(actualBytes).ToArray();
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
                SendImageChunk_ServerRpc(bytesToSend, chunkNumber, lastChunk, projectionMatrixAsArray, cameraToWorldMatrixAsArray, width, height);
                chunkNumber++;
            }
            yield return null;
        }
    }
    

    [ServerRpc(RequireOwnership = false)]
    void SendImageChunk_ServerRpc(byte[] bytes, int chunkNumber, bool lastChunk, float[] projectionMatrixAsArray,
        float[] cameraToWorldMatrixAsArray, int width, int height)
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
        projectionMatrix.SetRow(0, new Vector4(projectionMatrixAsArray[0], projectionMatrixAsArray[1], projectionMatrixAsArray[2], projectionMatrixAsArray[3]));
        projectionMatrix.SetRow(1, new Vector4(projectionMatrixAsArray[4], projectionMatrixAsArray[5], projectionMatrixAsArray[6], projectionMatrixAsArray[7]));
        projectionMatrix.SetRow(2, new Vector4(projectionMatrixAsArray[8], projectionMatrixAsArray[9], projectionMatrixAsArray[10], projectionMatrixAsArray[11]));
        projectionMatrix.SetRow(3, new Vector4(projectionMatrixAsArray[12], projectionMatrixAsArray[13], projectionMatrixAsArray[14], projectionMatrixAsArray[15]));
        
        var cameraToWorldMatrix = new Matrix4x4();
        cameraToWorldMatrix.SetRow(0, new Vector4(cameraToWorldMatrixAsArray[0], cameraToWorldMatrixAsArray[1], cameraToWorldMatrixAsArray[2], cameraToWorldMatrixAsArray[3]));
        cameraToWorldMatrix.SetRow(1, new Vector4(cameraToWorldMatrixAsArray[4], cameraToWorldMatrixAsArray[5], cameraToWorldMatrixAsArray[6], cameraToWorldMatrixAsArray[7]));
        cameraToWorldMatrix.SetRow(2, new Vector4(cameraToWorldMatrixAsArray[8], cameraToWorldMatrixAsArray[9], cameraToWorldMatrixAsArray[10], cameraToWorldMatrixAsArray[11]));
        cameraToWorldMatrix.SetRow(3, new Vector4(cameraToWorldMatrixAsArray[12], cameraToWorldMatrixAsArray[13], cameraToWorldMatrixAsArray[14], cameraToWorldMatrixAsArray[15]));
        
        
        CreatePositionedPhoto(projectionMatrix, cameraToWorldMatrix, width, height,
            _imageBufferList.ToArray());
    }

    public void CreatePositionedPhoto(Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix, int width, int height,
        byte[] imageBufferList)
    {
        //load imageBufferList as IMFMediaBuffer into texture
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.LoadImage(imageBufferList);
        texture.Apply();
        var positionedPhoto = Instantiate(PositionedPhotoPrefab);
        positionedPhoto.GetComponent<PositionedPhoto>()
            .Initialize(projectionMatrix, cameraToWorldMatrix, width, height, texture);
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


enum DrawingState
{
    Drawing,
    Waiting,
    NotDrawing
}

enum PlaneState
{
    NoPlane,
    MovePlane,
    StillPlane
}

public class DesktopDrawingHandler : MonoBehaviour
{
    private NetworkPen _networkPen;
    private DrawingState _drawingState = DrawingState.NotDrawing;
    private PlaneState _planeState = PlaneState.NoPlane;
    public GameObject PlanePrefab;
    private Transform _currentPlane;
    private Vector3 _planeMouseDownPosition;
    private Vector3 _previousMousePosition;
    public Canvas Canvas;
    public Button DrawButton;
    public Button ClearPlaneButton;
    public Button UndoButton;
    public Button RedButton;
    public Button GreenButton;
    public Button BlueButton;
    
    int UILayer;
    public void Start()
    {
        UILayer = LayerMask.NameToLayer("UI");
        _networkPen = FindObjectOfType<NetworkPen>();
        
        if (GlobalConfig.Singleton.MyBuildTarget == ArBuildTarget.Web)
        {
            Canvas.gameObject.SetActive(true);
        }
        else
        {
            Canvas.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    public void StartDrawMode()
    {
        _drawingState = DrawingState.Waiting;
        _networkPen.Show();
        GreenButton.gameObject.SetActive(true);
        RedButton.gameObject.SetActive(true);
        BlueButton.gameObject.SetActive(true);
        UndoButton.gameObject.SetActive(true);
    }

    public void StopDrawMode()
    {
        _drawingState = DrawingState.NotDrawing;
        _networkPen.Hide();
        if (_currentPlane != null)
        {
            Destroy(_currentPlane.gameObject);
            _currentPlane = null;
            _planeState = PlaneState.NoPlane;
        }
        GreenButton.gameObject.SetActive(false);
        RedButton.gameObject.SetActive(false);
        BlueButton.gameObject.SetActive(false);
        UndoButton.gameObject.SetActive(false);
        ClearPlaneButton.gameObject.SetActive(false);
    }

    public void Update()
    {
        if (_drawingState == DrawingState.Waiting)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                _networkPen.transform.position = hit.point+hit.normal*0.01f;
                _networkPen.SetSize(Vector3.Distance(Camera.main.transform.position, hit.point) * 0.1f);
            }
            else
            {
                _networkPen.SetSize(0f);
            }
            
            if (IsPointerOverUIElement())
            {
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                _drawingState = DrawingState.Drawing;
                _networkPen.StartDrawing();
            }
        }
        else if (_drawingState == DrawingState.Drawing)
        {
            if (IsPointerOverUIElement())
            {
                return;
            }
            if (Input.GetMouseButtonUp(0))
            {
                _drawingState = DrawingState.Waiting;
                _networkPen.StopDrawing();
                return;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                _networkPen.transform.position = hit.point+hit.normal*0.01f;
            }
        }

        if (_drawingState != DrawingState.NotDrawing)
        {
            switch (_planeState)
            {
                case PlaneState.NoPlane:
                    if (Input.GetKeyDown(KeyCode.LeftShift))
                    {
                        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out var hit))
                        {
                            //create quaternion with upwards being hit=>camera und right the cross product of hit and camera
                            var rotation = Quaternion.LookRotation(hit.normal,
                                Vector3.Cross(hit.normal, Camera.main.transform.right));
                            _currentPlane = Instantiate(PlanePrefab, hit.point, rotation).transform;
                            _planeState = PlaneState.MovePlane;
                            ClearPlaneButton.gameObject.SetActive(true);
                        }
                    }

                    break;
                case PlaneState.MovePlane:
                    if (Input.GetKeyUp(KeyCode.LeftShift))
                    {
                        _planeState = PlaneState.StillPlane;
                        break;
                    }

                    //rotate plane on mouse movement
                    var mouseDelta = Input.mousePosition - _previousMousePosition;

                    _currentPlane.RotateAround(_currentPlane.position, -Camera.main.transform.up, mouseDelta.x);
                    _currentPlane.RotateAround(_currentPlane.position, Camera.main.transform.right, mouseDelta.y);

                    break;
                case PlaneState.StillPlane:
                    if (Input.GetKeyDown(KeyCode.LeftShift))
                    {
                        //destroy plane and create new one at current raycast position
                        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out var hit))
                        {
                            var rotation = Quaternion.LookRotation(Camera.main.transform.forward,
                                Camera.main.transform.up);
                            Destroy(_currentPlane.gameObject);
                            _currentPlane = Instantiate(PlanePrefab, hit.point, rotation).transform;
                        }

                        _planeState = PlaneState.MovePlane;
                    }

                    break;
            }
        }

        _previousMousePosition = Input.mousePosition;
    }


    public void OnGUI()
    {
        
    }

    public void RemovePlane()
    {
        if (_currentPlane == null)
        {
            ClearPlaneButton.gameObject.SetActive(false);
            return;
        }
        Destroy(_currentPlane.gameObject);
        _currentPlane = null;
        _planeState = PlaneState.NoPlane;
        ClearPlaneButton.gameObject.SetActive(false);
    }
    
    public void SetColorRed()
    {
        _networkPen.Color = Color.red;
    }
    public void SetColorGreen()
    {
        _networkPen.Color = Color.green;
    }
    public void SetColorBlue()
    {
        _networkPen.Color = Color.blue;
    }
    public void ToggleDrawMode()
    {
        if (_drawingState == DrawingState.NotDrawing)
        {
            StartDrawMode();
        }
        else
        {
            StopDrawMode();
        }
    }
    public void Undo()
    {
        _networkPen.DeleteMyLastLine_ServerRpc();
    }
    
    
    
    //Returns 'true' if we touched or hovering on Unity UI element.
    public bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }
 
 
    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == UILayer)
                return true;
        }
        return false;
    }
 
 
    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}
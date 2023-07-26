using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

    public void Start()
    {
        _networkPen = FindObjectOfType<NetworkPen>();
    }

    public void StartDrawMode()
    {
        _drawingState = DrawingState.Waiting;
        _networkPen.Show();
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
    }

    public void Update()
    {
        if (_drawingState == DrawingState.Waiting)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                _networkPen.transform.position = hit.point;
            }

            if (Input.GetMouseButtonDown(0))
            {
                _drawingState = DrawingState.Drawing;
                _networkPen.StartDrawing();
            }
        }
        else if (_drawingState == DrawingState.Drawing)
        {
            if (Input.GetMouseButtonUp(0))
            {
                _drawingState = DrawingState.Waiting;
                _networkPen.StopDrawing();
                return;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                _networkPen.transform.position = hit.point;
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


    //add a button saying Draw or Stop Drawing to the UI bottom right
    public void OnGUI()
    {
        var bottomRightRect = new Rect(Screen.width - 110, Screen.height - 60, 100, 50);
        if (GUI.Button(bottomRightRect, _drawingState != DrawingState.NotDrawing ? "Stop Drawing" : "Draw"))
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

        if (_drawingState == DrawingState.NotDrawing)
        {
            return;
        }

        //5 pixel above the draw button show three color buttons
        var redButtonRect = new Rect(Screen.width - 110, Screen.height - 115, 30, 50);
        var greenButtonRect = new Rect(Screen.width - 75, Screen.height - 115, 30, 50);
        var blueButtonRect = new Rect(Screen.width - 40, Screen.height - 115, 30, 50);
        GUI.backgroundColor = Color.red;
        if (GUI.Button(redButtonRect, ""))
        {
            _networkPen.Color = Color.red;
        }

        GUI.backgroundColor = Color.green;
        if (GUI.Button(greenButtonRect, ""))
        {
            _networkPen.Color = Color.green;
        }

        GUI.backgroundColor = Color.blue;
        if (GUI.Button(blueButtonRect, ""))
        {
            _networkPen.Color = Color.blue;
        }

        //add a button above the color buttons to clear the plane if planestate is not NoPlane
        if (_planeState != PlaneState.NoPlane)
        {
            var clearPlaneRect = new Rect(Screen.width - 110, Screen.height - 170, 100, 50);
            GUI.backgroundColor = Color.white;
            if (GUI.Button(clearPlaneRect, "Clear Plane"))
            {
                Destroy(_currentPlane.gameObject);
                _currentPlane = null;
                _planeState = PlaneState.NoPlane;
            }
        }
    }
}
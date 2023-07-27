using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkLine : NetworkBehaviour
{
    public LineRenderer LineRenderer;

    public NetworkList<Vector3> Points = new NetworkList<Vector3>(new List<Vector3>(),
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<Color> Color = new NetworkVariable<Color>(UnityEngine.Color.green,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<float> Width = new NetworkVariable<float>(0.01f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Coefficient of optimization. The higher it is, the more lossy the optimization is.
    [Range(0, 1)] public float OptimizationCoefficient = 0.9f;
    public bool Optimize = true;
    public float MinDistance = 0.03f;

    private List<Vector3> _serverPoints = new List<Vector3>();

    //add new point and check if the point before can be removed because it does not add any new information based on the optimization coefficient
    public void AddPoint(Vector3 point)
    {
        _serverPoints.Add(point);
        var simplifiedPoints = new List<Vector3>();
        LineUtility.Simplify(_serverPoints, OptimizationCoefficient, simplifiedPoints);
        Points.Clear();
        foreach (var simplifiedPoint in simplifiedPoints)
        {
            Points.Add(simplifiedPoint);
        }
    }

    public void Reset()
    {
        Points.Clear();
    }

    private bool _isInitialized = false;

    public void Initialize()
    {
        _isInitialized = true;
        LineRenderer.positionCount = Points.Count;
        LineRenderer.SetPositions(Positions);
        Points.OnListChanged += OnPositionsChanged;
        Color.OnValueChanged += OnColorChanged;
        Width.OnValueChanged += OnWidthChanged;
    }

    private void OnColorChanged(Color previousValue, Color newValue)
    {
        LineRenderer.material.color = newValue;
    }

    private void OnWidthChanged(float previousValue, float newValue)
    {
        LineRenderer.widthCurve = new AnimationCurve(new Keyframe(0, Width.Value), new Keyframe(1, Width.Value));
    }

    private void OnPositionsChanged(NetworkListEvent<Vector3> changeEvent)
    {
        if (Points.Count < 2)
            return;
        LineRenderer.positionCount = Points.Count;
        LineRenderer.SetPositions(Positions);
    }

    //getter for positions using enumerator
    public Vector3[] Positions
    {
        get
        {
            var positions = new Vector3[Points.Count];
            using var enumerator = Points.GetEnumerator();
            var index = 0;
            while (enumerator.MoveNext())
            {
                positions[index] = enumerator.Current;
                index++;
            }

            return positions;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!_isInitialized)
            Initialize();
    }
}
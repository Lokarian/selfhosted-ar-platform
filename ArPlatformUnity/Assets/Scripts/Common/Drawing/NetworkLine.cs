using System;
using System.Collections;
using System.Collections.Generic;
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
    // Start is called before the first frame update
    public float Width = 0.1f;

    // Coefficient of optimization. The higher it is, the more lossy the optimization is.
    [Range(0, 1)] public float OptimizationCoefficient = 0.9f;
    public bool Optimize = true;
    public float MinDistance = 0.03f;

    //add new point and check if the point before can be removed because it does not add any new information based on the optimization coefficient
    public void AddPoint(Vector3 point)
    {
        if (Points.Count < 2)
        {
            Points.Add(point);
            return;
        }
        
        var middlePoint = Points[Points.Count - 1];
        var p1 = Points[Points.Count - 2];
        var p2 = point;
        var originalVector = p2 - p1;
        var middleVector1 = middlePoint - p1;
        var middleVector2 = middlePoint - p2;

        //if the angle between the original vector and the middle vector is smaller than the optimization coefficient we can remove the middle point
        var removeLast = Vector3.Dot(originalVector.normalized, middleVector1.normalized) > OptimizationCoefficient &&
                         Vector3.Dot(originalVector.normalized, middleVector2.normalized) > OptimizationCoefficient;

        var distance = removeLast ? Vector3.Distance(p1, p2) : Vector3.Distance(middlePoint, p2);
        if (distance > MinDistance)
        {
            if (removeLast)
            {
                Points.RemoveAt(Points.Count - 1);
            }

            Points.Add(point);
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
        LineRenderer.widthCurve = new AnimationCurve(new Keyframe(0, Width), new Keyframe(1, Width));
        LineRenderer.positionCount = Points.Count;
        LineRenderer.SetPositions(Positions);
        Points.OnListChanged += OnPositionsChanged;
        Color.OnValueChanged += OnColorChanged;
    }
    private void OnColorChanged(Color previousValue, Color newValue)
    {
        LineRenderer.startColor = newValue;
        LineRenderer.endColor = newValue;
    }
    private void OnPositionsChanged(NetworkListEvent<Vector3> changeEvent)
    {
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
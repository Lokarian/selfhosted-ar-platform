using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Subsystems;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;

public class NetworkHand : NetworkBehaviour
{
    readonly NetworkVariable<bool> _isHandVisible = new(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

    [SerializeField] [Tooltip("The XRNode on which this hand is located.")]
    private XRNode handNode = XRNode.LeftHand;

    public XRNode HandNode
    {
        get => handNode;
        set => handNode = value;
    }

    private HandsAggregatorSubsystem handsSubsystem;

    private Transform[] jointTransforms;
    private bool _ready = false;
    private Renderer[] _renderers;

    private void Start()
    {
        //get all direct children of this object
        jointTransforms = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            jointTransforms[i] = transform.GetChild(i);
        }

        _renderers = GetComponentsInChildren<Renderer>();
        _isHandVisible.OnValueChanged += OnHandVisibilityChanged;
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsLocalPlayer)
        {
            return;
        }
        Debug.Assert(handNode == XRNode.LeftHand || handNode == XRNode.RightHand,
            $"HandVisualizer has an invalid XRNode ({handNode})!");

        handsSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();

        if (handsSubsystem == null)
        {
            StartCoroutine(EnableWhenSubsystemAvailable());
        }
        else
        {
            _ready = true;
        }
    }


    private IEnumerator EnableWhenSubsystemAvailable()
    {
        yield return new WaitUntil(
            () => XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>() != null);
        _ready = true;
    }

    private void Update()
    {
        if (_ready)
            UpdateHandJoints();
    }

    private void OnHandVisibilityChanged(bool previousvalue, bool newvalue)
    {
        if (newvalue)
        {
            if (IsOwner)
            {
                //if we are the owner, we don't need to render the hand, they are only for other players
                return;
            }
            foreach (var renderer1 in _renderers)
            {
                renderer1.enabled = true;
            }
        }
        else
        {
            foreach (var renderer1 in _renderers)
            {
                renderer1.enabled = false;
            }
        }
    }

    private void UpdateHandJoints()
    {
        if (handsSubsystem.TryGetEntireHand(handNode, out var jointPoses))
        {
            _isHandVisible.Value = true;
            if (jointTransforms.Length != jointPoses.Count)
            {
                Debug.LogWarning(
                    $"HandVisualizer has {jointTransforms.Length} joints, but {jointPoses.Count} joint poses were provided!");
                return;
            }

            for (int i = 0; i < jointPoses.Count; i++)
            {
                jointTransforms[i].position = jointPoses[i].Position;
                jointTransforms[i].localScale =jointPoses[i].Radius * Vector3.one;
            }
        }
        else
        {
            _isHandVisible.Value = false;
        }
    }
}
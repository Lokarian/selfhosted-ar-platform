using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using SpatialAwarenessHandler = Microsoft.MixedReality.Toolkit.SpatialAwareness.IMixedRealitySpatialAwarenessObservationHandler<Microsoft.MixedReality.Toolkit.SpatialAwareness.SpatialAwarenessMeshObject>;



[AddComponentMenu("Scripts/MRTK/Examples/DemoSpatialMeshHandler")]
public class EnvironmentMeshHandler : MonoBehaviour,SpatialAwarenessHandler
{
    /// <summary>
    /// Collection that tracks the IDs and count of updates for each active spatial awareness mesh.
    /// </summary>
    private Dictionary<int, uint> meshUpdateData = new Dictionary<int, uint>();

    /// <summary>
    /// Value indicating whether or not this script has registered for spatial awareness events.
    /// </summary>
    protected bool isRegistered = false;

    protected virtual void Start()
    {
        RegisterEventHandlers<SpatialAwarenessHandler, SpatialAwarenessMeshObject>();
    }

    protected virtual void OnEnable()
    {
        RegisterEventHandlers<SpatialAwarenessHandler, SpatialAwarenessMeshObject>();
    }

    protected virtual void OnDisable()
    {
        UnregisterEventHandlers<SpatialAwarenessHandler, SpatialAwarenessMeshObject>();
    }

    protected virtual void OnDestroy()
    {
        UnregisterEventHandlers<SpatialAwarenessHandler, SpatialAwarenessMeshObject>();
    }
    
    /// <summary>
        /// Registers for the spatial awareness system events.
        /// </summary>
        protected virtual void RegisterEventHandlers<T, U>()
            where T : IMixedRealitySpatialAwarenessObservationHandler<U>
            where U : BaseSpatialAwarenessObject
        {
            if (!isRegistered && (CoreServices.SpatialAwarenessSystem != null))
            {
                CoreServices.SpatialAwarenessSystem.RegisterHandler<T>(this);
                isRegistered = true;
            }
        }

        /// <summary>
        /// Unregisters from the spatial awareness system events.
        /// </summary>
        protected virtual void UnregisterEventHandlers<T, U>()
            where T : IMixedRealitySpatialAwarenessObservationHandler<U>
            where U : BaseSpatialAwarenessObject
        {
            if (isRegistered && (CoreServices.SpatialAwarenessSystem != null))
            {
                CoreServices.SpatialAwarenessSystem.UnregisterHandler<T>(this);
                isRegistered = false;
            }
        }

        /// <inheritdoc />
        public virtual void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
        {
            AddToData(eventData.Id);
        }

        /// <inheritdoc />
        public virtual void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
        {
            UpdateData(eventData.Id);
            var mesh= eventData.SpatialObject.Filter.mesh;
            //mesh.
        }

        /// <inheritdoc />
        public virtual void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
        {
            RemoveFromData(eventData.Id);
        }

        /// <summary>
        /// Records the mesh id when it is first added.
        /// </summary>
        protected void AddToData(int eventDataId)
        {
            // A new mesh has been added.
            Debug.Log($"Started tracking mesh {eventDataId}");
            meshUpdateData.Add(eventDataId, 0);
        }

        /// <summary>
        /// Increments the update count of the mesh with the id.
        /// </summary>
        protected void UpdateData(int eventDataId)
        {
            // A mesh has been updated. Find it and increment the update count.
            if (meshUpdateData.TryGetValue(eventDataId, out uint updateCount))
            {
                // Set the new update count.
                meshUpdateData[eventDataId] = ++updateCount;

                Debug.Log($"Mesh {eventDataId} has been updated {updateCount} times.");
            }
        }

        /// <summary>
        /// Removes the mesh id.
        /// </summary>
        protected void RemoveFromData(int eventDataId)
        {
            // A mesh has been removed. We no longer need to track the count of updates.
            if (meshUpdateData.ContainsKey(eventDataId))
            {
                Debug.Log($"No longer tracking mesh {eventDataId}.");
                meshUpdateData.Remove(eventDataId);
            }
        }
}
#if UNIFIED_NETCODE
using Unity.NetCode;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// TODO-UNIFIED: Needs further peer review and exploring alternate ways of handling this.
    /// This is a component that is added to the root of all N4E-spawned hybrid prefab instances. It is used to link
    /// <see cref="NetworkObject.SerializedObject"/> the N4E-spawned hybrid prefab instances to the incoming <see cref="CreateObjectMessage"/>
    /// specific to the N4E-spawned hybrid prefab instance that has the matching <see cref="NetworkObjectId"/>.
    /// </summary>

    [DefaultExecutionOrder(GhostObjectExecutionOrder.ExecutionOrder + 1)]
    //BREAK --- Fix this on UNIFIED side 1st
    public partial class NetworkObjectBridge : GhostBehaviour
    {
        // DefaultExecutionOrder
        // TODO: Define a const for the value used on GhostAdapter and use that value
        // to set the execution order so if it changes on GhostAdapter it updates here.
#if UNITY_EDITOR
        private void OnValidate()
        {
            hideFlags = HideFlags.HideInInspector;

            var ghostAdapter = GetComponent<GhostObject>();
            if (ghostAdapter == null)
            {
                return;
            }

            // Start users with just interpolation (they can adjust this if they want prediction)
            // to make the initial transition less problematic for users.
            ghostAdapter.SupportedGhostModes = GhostModeMask.Interpolated;

#if COM_UNITY_MODULES_PHYSICS
            var rigidBody = GetComponent<Rigidbody>();
            var ghostRigidBody = GetComponent<GhostRigidbody>();
            if (rigidBody != null)
            {
                // This must be enabled when replicating the rigid body.

                ghostAdapter.SingleWorldHostInterpolationSmoothing = SingleWorldHostInterpolationMode.Interpolate;
                // TODO: Currently, this is added only if you enable replication of the rigid body.
                // There is a bug where if you don't add this component it doesn't synchronize the transform.
                // Remove this once the issue is resolved.
                if (ghostRigidBody == null)
                {
                    gameObject.AddComponent<GhostRigidbody>();
                }
            }
#endif
#if COM_UNITY_MODULES_PHYSICS2D
            // TODO: Fill out a similar script as above but for the 2D version
#endif
        }
#endif

        /// <summary>
        /// This is used to link <see cref="NetworkObject.SerializedObject"/> data to
        /// N4E-spawned hybrid prefab instances.
        /// </summary>
        internal GhostField<ulong> NetworkObjectId = new GhostField<ulong>();
        public void SetNetworkObjectId(ulong networkObjectId)
        {
            NetworkObjectId.PresetValue(networkObjectId);
            NetworkObjectId.Value = networkObjectId;
        }

        /// <summary>
        /// Currently, NGO provides the parenting event handling via <see cref="ParentSyncMessage"/>.
        /// Once <see cref="GhostField{InternalTypeT}"/> can provide a form of event notification that
        /// the value has changed, we can then invert this flow such that the change in the parent value
        /// drives the event.
        /// </summary>
        /// <param name="scale">We use NGO scale, delivered via ParentSyncMessage, that is applied to this
        /// instance's entity's PostTransformMatrix.</param>
        internal void HybridParentUpdate(Vector3 scale)
        {
            var current = Ghost.GetPositionAndRotation();
            //Debug.Log($"---- Current LT: {current.Position} | {current.Rotation.eulerAngles}");
            //Debug.Log($"---- New LT: {transform.localPosition} | {transform.localRotation}");
            Ghost.ApplyPostTransformMatrixScale(scale);
        }

        internal void ApplyScale(Vector3 scale)
        {
            Ghost.ApplyPostTransformMatrixScale(scale);
        }
    }
}
#endif

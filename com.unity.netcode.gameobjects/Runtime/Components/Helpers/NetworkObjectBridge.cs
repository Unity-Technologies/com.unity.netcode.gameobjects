#if UNIFIED_NETCODE
using Unity.NetCode;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// TODO-UNIFIED: Needs further peer review and exploring alternate ways of handling this.
    /// This is a component that is added to the root of all N4E-spawned hybrid prefab instances. It is used to link
    /// <see cref="NetworkObject.SerializedObject"/> the N4E-spawned hybrid prefab instances to the incoming <see cref="CreateObjectMessage"/>
    /// specific to the N4E-spawned hybrid prefab instance that has the matching <see cref="NetworkObjectId"/>.
    /// </summary>
    public partial class NetworkObjectBridge : GhostBehaviour
    {
#if UNITY_EDITOR
        [HideInInspector]
        [SerializeField]
        private bool m_Sorted = false;
        private void OnValidate()
        {
            // TODO-UNIFIED: GhostAdapter must be above all GhostBehaviours in order to assure the GhostAdapter is initialized before any GhostBehaviour.
            // This auto-sorting is required because the GhostBehaviours rely on the GhostAdapter.Awake being invoked before any GhostBehaviour.Awake.
            if (!m_Sorted && !EditorApplication.isPlaying)
            {
                while (UnityEditorInternal.ComponentUtility.MoveComponentUp(this))
                {
                    // Keep moving until it can't go higher
                }
                var ghostAdapter = gameObject.GetComponent<GhostAdapter>();
                // Now move the GhostAdapter to the top so it is above NetworkObjectBridge
                while (ghostAdapter != null && UnityEditorInternal.ComponentUtility.MoveComponentUp(ghostAdapter))
                {
                    // Keep moving until it can't go higher
                }

                m_Sorted = true;
            }
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
    }
}
#endif

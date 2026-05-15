#if UNIFIED_NETCODE
using Unity.NetCode;
using UnityEditor;

namespace Unity.Netcode
{
    /// <summary>
    /// TODO-UNIFIED: Needs further peer review and exploring alternate ways of handling this.
    /// </summary>
    /// <remarks>
    /// If used, we most likely would make this internal
    /// </remarks>
    public partial class NetworkObjectBridge : GhostBehaviour
    {

#if UNITY_EDITOR
        [UnityEngine.HideInInspector]
        [UnityEngine.SerializeField]
        private bool m_Sorted = false;
        private void OnValidate()
        {
            // Sort only once when we have first been added.
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
    }
}
#endif

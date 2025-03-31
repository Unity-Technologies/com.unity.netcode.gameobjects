using Unity.Netcode.Components;
using UnityEditor;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// The <see cref="CustomEditor"/> for <see cref="AnticipatedNetworkTransform"/>
    /// </summary>
    [CustomEditor(typeof(AnticipatedNetworkTransform), true)]
    public class AnticipatedNetworkTransformEditor : NetworkTransformEditor
    {
        /// <summary>
        /// Gets a value indicating whether the interpolate value should be hidden in the inspector.
        /// </summary>
        public override bool HideInterpolateValue => true;
    }
}

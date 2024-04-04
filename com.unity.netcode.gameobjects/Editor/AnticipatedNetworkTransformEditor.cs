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
        public override bool HideInterpolateValue => true;
    }
}

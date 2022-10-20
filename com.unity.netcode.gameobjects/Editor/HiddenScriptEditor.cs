using Unity.Netcode.Components;
#if UNITY_UNET_PRESENT
using Unity.Netcode.Transports.UNET;
#endif
using Unity.Netcode.Transports.UTP;
using UnityEditor;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// Internal use. Hides the script field for the given component.
    /// </summary>
    public class HiddenScriptEditor : UnityEditor.Editor
    {
        private static readonly string[] k_HiddenFields = { "m_Script" };

        /// <summary>
        /// Draws inspector properties without the script field.
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();
            DrawPropertiesExcluding(serializedObject, k_HiddenFields);
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }
    }
#if UNITY_UNET_PRESENT
    /// <summary>
    /// Internal use. Hides the script field for UNetTransport.
    /// </summary>
    [CustomEditor(typeof(UNetTransport), true)]
    public class UNetTransportEditor : HiddenScriptEditor
    {

    }
#endif

    /// <summary>
    /// Internal use. Hides the script field for UnityTransport.
    /// </summary>
    [CustomEditor(typeof(UnityTransport), true)]
    public class UnityTransportEditor : HiddenScriptEditor
    {

    }

#if COM_UNITY_MODULES_ANIMATION
    /// <summary>
    /// Internal use. Hides the script field for NetworkAnimator.
    /// </summary>
    [CustomEditor(typeof(NetworkAnimator), true)]
    public class NetworkAnimatorEditor : HiddenScriptEditor
    {

    }
#endif

#if COM_UNITY_MODULES_PHYSICS
    /// <summary>
    /// Internal use. Hides the script field for NetworkRigidbody.
    /// </summary>
    [CustomEditor(typeof(NetworkRigidbody), true)]
    public class NetworkRigidbodyEditor : HiddenScriptEditor
    {

    }
#endif

#if COM_UNITY_MODULES_PHYSICS2D
    /// <summary>
    /// Internal use. Hides the script field for NetworkRigidbody2D.
    /// </summary>
    [CustomEditor(typeof(NetworkRigidbody2D), true)]
    public class NetworkRigidbody2DEditor : HiddenScriptEditor
    {

    }
#endif
}

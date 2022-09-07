using Unity.Netcode.Components;
using Unity.Netcode.Transports.UNET;
using Unity.Netcode.Transports.UTP;
using UnityEditor;

namespace Unity.Netcode.Editor
{
    public class HiddenScriptEditor : UnityEditor.Editor
    {
        private static readonly string[] k_HiddenFields = { "m_Script" };
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();
            DrawPropertiesExcluding(serializedObject, k_HiddenFields);
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }
    }

    [CustomEditor(typeof(UNetTransport), true)]
    public class UNetTransportEditor : HiddenScriptEditor
    {

    }

    [CustomEditor(typeof(UnityTransport), true)]
    public class UnityTransportEditor : HiddenScriptEditor
    {

    }

#if COM_UNITY_MODULES_ANIMATION
    [CustomEditor(typeof(NetworkAnimator), true)]
    public class NetworkAnimatorEditor : HiddenScriptEditor
    {

    }
#endif

#if COM_UNITY_MODULES_PHYSICS
    [CustomEditor(typeof(NetworkRigidbody), true)]
    public class NetworkRigidbodyEditor : HiddenScriptEditor
    {

    }
#endif

#if COM_UNITY_MODULES_PHYSICS2D
    [CustomEditor(typeof(NetworkRigidbody2D), true)]
    public class NetworkRigidbody2DEditor : HiddenScriptEditor
    {

    }
#endif
}

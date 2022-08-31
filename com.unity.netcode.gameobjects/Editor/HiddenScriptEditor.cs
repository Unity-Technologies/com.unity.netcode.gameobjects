using Unity.Netcode.Components;
using Unity.Netcode.Transports.UNET;
using Unity.Netcode.Transports.UTP;
using UnityEditor;

namespace Unity.Netcode.Editor
{
    public class HiddenScriptEditor : UnityEditor.Editor
    {
        private static readonly string[] s_HiddenFields = {"m_Script"};
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();
            DrawPropertiesExcluding(serializedObject, s_HiddenFields);
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

    [CustomEditor(typeof(NetworkAnimator), true)]
    public class NetworkAnimatorEditor : HiddenScriptEditor
    {
        
    }

    [CustomEditor(typeof(NetworkRigidbody), true)]
    public class NetworkRigidbodyEditor : HiddenScriptEditor
    {
        
    }

    [CustomEditor(typeof(NetworkRigidbody2D), true)]
    public class NetworkRigidbody2DEditor : HiddenScriptEditor
    {
        
    }
}

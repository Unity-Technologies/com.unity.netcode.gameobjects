using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    [CustomEditor(typeof(NetworkAnimator), true)]
    [CanEditMultipleObjects]
    public class NetworkAnimatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            var label = new GUIContent("Animator", "The Animator component to synchronize");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Animator"), label);
            EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();
        }
    }
}

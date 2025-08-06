using Unity.Netcode.Components;
using UnityEditor;
//using UnityEngine;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// The <see cref="CustomEditor"/> for <see cref="NetworkAnimator"/>
    /// </summary>
    [CustomEditor(typeof(NetworkAnimator), true)]
    [CanEditMultipleObjects]
    public class NetworkAnimatorEditor : NetcodeEditorBase<NetworkAnimator>    
    {

        private SerializedProperty m_AuthorityMode;
        private SerializedProperty m_Animator;

        public override void OnEnable()
        {
            m_AuthorityMode = serializedObject.FindProperty(nameof(NetworkAnimator.AuthorityMode));

            m_Animator = serializedObject.FindProperty("m_Animator");
        }

        private void DisplayNetworkAnimatorProperties()
        {
            var networkAnimator = target as NetworkAnimator;
            EditorGUILayout.PropertyField(m_AuthorityMode);
            EditorGUILayout.PropertyField(m_Animator);
            if (networkAnimator.Animator != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animator Parameters", EditorStyles.boldLabel);
                {
                    // Add parameter list here
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var networkAnimator = target as NetworkAnimator;
            void SetExpanded(bool expanded) { networkAnimator.NetworkAnimatorExpanded = expanded; }
            DrawFoldOutGroup<NetworkTransform>(networkAnimator.GetType(), DisplayNetworkAnimatorProperties, networkAnimator.NetworkAnimatorExpanded, SetExpanded);
            base.OnInspectorGUI();
        }
    }
}

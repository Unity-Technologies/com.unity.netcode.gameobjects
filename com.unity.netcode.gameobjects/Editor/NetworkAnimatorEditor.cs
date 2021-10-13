using System;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    public static class TextUtility
    {
        public static GUIContent TextContent(string name, string tooltip)
        {
            var newContent = new GUIContent(name);
            newContent.tooltip = tooltip;
            return newContent;
        }

        public static GUIContent TextContent(string name)
        {
            return new GUIContent(name);
        }
    }

    [CustomEditor(typeof(NetworkAnimator), true)]
    [CanEditMultipleObjects]
    public class NetworkAnimatorEditor : UnityEditor.Editor
    {
        private NetworkAnimator m_AnimSync;
        [NonSerialized] private bool m_Initialized;
        private SerializedProperty m_AnimatorProperty;
        private GUIContent m_AnimatorLabel;

        private void Init()
        {
            if (m_Initialized)
            {
                return;
            }

            m_Initialized = true;
            m_AnimSync = target as NetworkAnimator;

            m_AnimatorProperty = serializedObject.FindProperty("m_Animator");
            m_AnimatorLabel = TextUtility.TextContent("Animator", "The Animator component to synchronize.");
        }

        public override void OnInspectorGUI()
        {
            Init();
            serializedObject.Update();
            DrawControls();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawControls()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_AnimatorProperty, m_AnimatorLabel);
            if (EditorGUI.EndChangeCheck())
            {
                m_AnimSync.ResetParameterOptions();
            }

            if (m_AnimSync.Animator == null)
            {
                return;
            }

            var controller = m_AnimSync.Animator.runtimeAnimatorController as AnimatorController;
            if (controller != null)
            {
                var showWarning = false;
                EditorGUI.indentLevel += 1;
                int i = 0;

                foreach (var p in controller.parameters)
                {
                    if (i >= NetworkAnimator.K_MaxAnimationParams)
                    {
                        showWarning = true;
                        break;
                    }

                    bool oldSend = m_AnimSync.GetParameterAutoSend(i);
                    bool send = EditorGUILayout.Toggle(p.name, oldSend);
                    if (send != oldSend)
                    {
                        m_AnimSync.SetParameterAutoSend(i, send);
                        EditorUtility.SetDirty(target);
                    }
                    i += 1;
                }

                if (showWarning)
                {
                    EditorGUILayout.HelpBox($"NetworkAnimator can only select between the first {NetworkAnimator.K_MaxAnimationParams} parameters in a mecanim controller", MessageType.Warning);
                }

                EditorGUI.indentLevel -= 1;
            }
        }
    }
}

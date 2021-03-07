using System;
using MLAPI.Prototyping;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkAnimator), true)]
    [CanEditMultipleObjects]
    public class NetworkAnimatorEditor : Editor
    {
        private NetworkAnimator m_Target;

        [NonSerialized]
        private bool m_Initialized;

        private SerializedProperty m_AnimatorProperty;
        private GUIContent m_AnimatorLabel;

        private void Initialize()
        {
            if (m_Initialized) return;

            m_Initialized = true;
            m_Target = target as NetworkAnimator;

            m_AnimatorProperty = serializedObject.FindProperty("m_Animator");
            m_AnimatorLabel = new GUIContent("Animator", "The Animator component to synchronize.");
        }

        private void DrawControls()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_AnimatorProperty, m_AnimatorLabel);
            if (EditorGUI.EndChangeCheck()) m_Target.ResetTrackedParams();

            var animator = m_Target.Animator;
            if (ReferenceEquals(animator, null)) return;

            var animatorController = animator.runtimeAnimatorController as AnimatorController;
            if (ReferenceEquals(animatorController, null)) return;

            EditorGUI.indentLevel += 1;
            var showWarning = false;
            {
                int paramIndex = 0;
                foreach (var animParam in animatorController.parameters)
                {
                    if (paramIndex >= 32)
                    {
                        showWarning = true;
                        break;
                    }

                    bool wasTracking = m_Target.GetParamTracking(paramIndex);
                    bool isTracking = EditorGUILayout.Toggle(animParam.name, wasTracking);
                    if (isTracking != wasTracking)
                    {
                        m_Target.SetParamTracking(paramIndex, isTracking);
                        EditorUtility.SetDirty(target);
                    }

                    paramIndex++;
                }
            }
            if (showWarning) EditorGUILayout.HelpBox("NetworkAnimator can only select between the first 32 parameters in a mecanim controller", MessageType.Warning);
            EditorGUI.indentLevel -= 1;
        }

        public override void OnInspectorGUI()
        {
            Initialize();
            serializedObject.Update();
            DrawControls();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
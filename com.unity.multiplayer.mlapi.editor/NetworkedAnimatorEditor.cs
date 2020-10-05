using System;
using MLAPI.Prototyping;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkedAnimator), true)]
    [CanEditMultipleObjects]
    public class NetworkAnimatorEditor : Editor
    {
        private NetworkedAnimator networkedAnimatorTarget;
        [NonSerialized]
        private bool initialized;

        private SerializedProperty animatorProperty;
        private GUIContent animatorLabel;

        void Init()
        {
            if (initialized)
                return;

            initialized = true;
            networkedAnimatorTarget = target as NetworkedAnimator;

            animatorProperty = serializedObject.FindProperty("_animator");
            animatorLabel = new GUIContent("Animator", "The Animator component to synchronize.");
        }

        public override void OnInspectorGUI()
        {
            Init();
            serializedObject.Update();
            DrawControls();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawControls()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animatorProperty, animatorLabel);

            if (EditorGUI.EndChangeCheck())
                networkedAnimatorTarget.ResetParameterOptions();

            if (networkedAnimatorTarget.animator == null)
                return;

            var controller = networkedAnimatorTarget.animator.runtimeAnimatorController as AnimatorController;
            if (controller != null)
            {
                var showWarning = false;
                EditorGUI.indentLevel += 1;
                int i = 0;

                foreach (var p in controller.parameters)
                {
                    if (i >= 32)
                    {
                        showWarning = true;
                        break;
                    }

                    bool oldSend = networkedAnimatorTarget.GetParameterAutoSend(i);
                    bool send = EditorGUILayout.Toggle(p.name, oldSend);
                    if (send != oldSend)
                    {
                        networkedAnimatorTarget.SetParameterAutoSend(i, send);
                        EditorUtility.SetDirty(target);
                    }
                    i += 1;
                }

                if (showWarning)
                    EditorGUILayout.HelpBox("NetworkAnimator can only select between the first 32 parameters in a mecanim controller", MessageType.Warning);

                EditorGUI.indentLevel -= 1;
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Separator();
                if (networkedAnimatorTarget.param0 != "")
                    EditorGUILayout.LabelField("Param 0", networkedAnimatorTarget.param0);
                if (networkedAnimatorTarget.param1 != "")
                    EditorGUILayout.LabelField("Param 1", networkedAnimatorTarget.param1);
                if (networkedAnimatorTarget.param2 != "")
                    EditorGUILayout.LabelField("Param 2", networkedAnimatorTarget.param2);
                if (networkedAnimatorTarget.param3 != "")
                    EditorGUILayout.LabelField("Param 3", networkedAnimatorTarget.param3);
                if (networkedAnimatorTarget.param4 != "")
                    EditorGUILayout.LabelField("Param 4", networkedAnimatorTarget.param4);
            }
        }
    }
}

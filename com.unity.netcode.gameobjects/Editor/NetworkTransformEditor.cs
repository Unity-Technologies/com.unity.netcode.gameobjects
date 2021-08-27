using Unity.Netcode.Prototyping;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    [CustomEditor(typeof(NetworkTransform))]
    public class NetworkTransformEditor : UnityEditor.Editor
    {
        private NetworkTransform m_NetworkTransform;
        private SerializedProperty m_InLocalSpaceProperty;
        private SerializedProperty m_PositionThresholdProperty;
        private SerializedProperty m_RotAngleThresholdProperty;
        private SerializedProperty m_ScaleThresholdProperty;

        private static int s_ToggleOffset = 45;
        private static float s_MaxRowWidth = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + 5;
        private static GUIContent s_PositionLabel = EditorGUIUtility.TrTextContent("Position");
        private static GUIContent s_RotationLabel = EditorGUIUtility.TrTextContent("Rotation");
        private static GUIContent s_ScaleLabel = EditorGUIUtility.TrTextContent("Scale");

        public void OnEnable()
        {
            m_NetworkTransform = target as NetworkTransform;

            m_InLocalSpaceProperty = serializedObject.FindProperty(nameof(NetworkTransform.InLocalSpace));

            m_PositionThresholdProperty = serializedObject.FindProperty(nameof(NetworkTransform.PositionThreshold));
            m_RotAngleThresholdProperty = serializedObject.FindProperty(nameof(NetworkTransform.RotAngleThreshold));
            m_ScaleThresholdProperty = serializedObject.FindProperty(nameof(NetworkTransform.ScaleThreshold));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_InLocalSpaceProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Syncing", EditorStyles.boldLabel);
            {
                GUILayout.BeginHorizontal();

                var rect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, s_MaxRowWidth, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight, EditorStyles.numberField);
                var ctid = GUIUtility.GetControlID(7231, FocusType.Keyboard, rect);

                rect = EditorGUI.PrefixLabel(rect, ctid, s_PositionLabel);
                rect.width = s_ToggleOffset;

                EditorGUI.ToggleLeft(rect, "X", true);
                rect.x += s_ToggleOffset;
                EditorGUI.ToggleLeft(rect, "Y", true);
                rect.x += s_ToggleOffset;
                EditorGUI.ToggleLeft(rect, "Z", true);

                GUILayout.EndHorizontal();
            }
            {
                GUILayout.BeginHorizontal();

                var rect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, s_MaxRowWidth, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight, EditorStyles.numberField);
                var ctid = GUIUtility.GetControlID(7231, FocusType.Keyboard, rect);

                rect = EditorGUI.PrefixLabel(rect, ctid, s_RotationLabel);
                rect.width = s_ToggleOffset;

                EditorGUI.ToggleLeft(rect, "X", true);
                rect.x += s_ToggleOffset;
                EditorGUI.ToggleLeft(rect, "Y", true);
                rect.x += s_ToggleOffset;
                EditorGUI.ToggleLeft(rect, "Z", true);

                GUILayout.EndHorizontal();
            }
            {
                GUILayout.BeginHorizontal();

                var rect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, s_MaxRowWidth, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight, EditorStyles.numberField);
                var ctid = GUIUtility.GetControlID(7231, FocusType.Keyboard, rect);

                rect = EditorGUI.PrefixLabel(rect, ctid, s_ScaleLabel);
                rect.width = s_ToggleOffset;

                EditorGUI.ToggleLeft(rect, "X", true);
                rect.x += s_ToggleOffset;
                EditorGUI.ToggleLeft(rect, "Y", true);
                rect.x += s_ToggleOffset;
                EditorGUI.ToggleLeft(rect, "Z", true);

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Thresholds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PositionThresholdProperty);
            EditorGUILayout.PropertyField(m_RotAngleThresholdProperty);
            EditorGUILayout.PropertyField(m_ScaleThresholdProperty);
        }
    }
}

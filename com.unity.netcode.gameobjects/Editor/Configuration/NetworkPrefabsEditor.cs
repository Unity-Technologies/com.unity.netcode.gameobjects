using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    [CustomEditor(typeof(NetworkPrefabsList), true)]
    [CanEditMultipleObjects]
    public class NetworkPrefabsEditor : UnityEditor.Editor
    {
        private ReorderableList m_NetworkPrefabsList;
        private SerializedProperty m_IsDefaultBool;

        private void OnEnable()
        {
            m_IsDefaultBool = serializedObject.FindProperty(nameof(NetworkPrefabsList.IsDefault));
            m_NetworkPrefabsList = new ReorderableList(serializedObject, serializedObject.FindProperty("List"), true, true, true, true);
            m_NetworkPrefabsList.elementHeightCallback = index =>
            {
                var networkOverrideInt = 0;
                if (m_NetworkPrefabsList.count > 0)
                {
                    var networkPrefab = m_NetworkPrefabsList.serializedProperty.GetArrayElementAtIndex(index);
                    var networkOverrideProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.Override));
                    networkOverrideInt = networkOverrideProp.enumValueIndex;
                }

                return 8 + (networkOverrideInt == 0 ? EditorGUIUtility.singleLineHeight : (EditorGUIUtility.singleLineHeight * 2) + 5);
            };
            m_NetworkPrefabsList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.y += 5;

                var networkPrefab = m_NetworkPrefabsList.serializedProperty.GetArrayElementAtIndex(index);
                var networkPrefabProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.Prefab));
                var networkSourceHashProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.SourceHashToOverride));
                var networkSourcePrefabProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.SourcePrefabToOverride));
                var networkTargetPrefabProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.OverridingTargetPrefab));
                var networkOverrideProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.Override));
                var networkOverrideInt = networkOverrideProp.enumValueIndex;
                var networkOverrideEnum = (NetworkPrefabOverride)networkOverrideInt;
                EditorGUI.LabelField(new Rect(rect.x + rect.width - 70, rect.y, 60, EditorGUIUtility.singleLineHeight), "Override");
                if (networkOverrideEnum == NetworkPrefabOverride.None)
                {
                    if (EditorGUI.Toggle(new Rect(rect.x + rect.width - 15, rect.y, 10, EditorGUIUtility.singleLineHeight), false))
                    {
                        networkOverrideProp.enumValueIndex = (int)NetworkPrefabOverride.Prefab;
                    }
                }
                else
                {
                    if (!EditorGUI.Toggle(new Rect(rect.x + rect.width - 15, rect.y, 10, EditorGUIUtility.singleLineHeight), true))
                    {
                        networkOverrideProp.enumValueIndex = 0;
                        networkOverrideEnum = NetworkPrefabOverride.None;
                    }
                }

                if (networkOverrideEnum == NetworkPrefabOverride.None)
                {
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 80, EditorGUIUtility.singleLineHeight), networkPrefabProp, GUIContent.none);
                }
                else
                {
                    networkOverrideProp.enumValueIndex = GUI.Toolbar(new Rect(rect.x, rect.y, 100, EditorGUIUtility.singleLineHeight), networkOverrideInt - 1, new[] { "Prefab", "Hash" }) + 1;

                    if (networkOverrideEnum == NetworkPrefabOverride.Prefab)
                    {
                        EditorGUI.PropertyField(new Rect(rect.x + 110, rect.y, rect.width - 190, EditorGUIUtility.singleLineHeight), networkSourcePrefabProp, GUIContent.none);
                    }
                    else
                    {
                        EditorGUI.PropertyField(new Rect(rect.x + 110, rect.y, rect.width - 190, EditorGUIUtility.singleLineHeight), networkSourceHashProp, GUIContent.none);
                    }

                    rect.y += EditorGUIUtility.singleLineHeight + 5;

                    EditorGUI.LabelField(new Rect(rect.x, rect.y, 100, EditorGUIUtility.singleLineHeight), "Overriding Prefab");
                    EditorGUI.PropertyField(new Rect(rect.x + 110, rect.y, rect.width - 110, EditorGUIUtility.singleLineHeight), networkTargetPrefabProp, GUIContent.none);
                }
            };
            m_NetworkPrefabsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "NetworkPrefabs");
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_IsDefaultBool);
            }

            m_NetworkPrefabsList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;

using MLAPI.SceneManagement;

namespace MLAPI.Editor
{
    [CustomEditor(typeof(SceneRegistration), true)]
    [CanEditMultipleObjects]
    public class SceneRegistrationEditor : UnityEditor.Editor
    {
        private ReorderableList m_SceneEntryList;

        private SceneRegistration m_SceneRegistration;

        private SerializedProperty m_NetworkManagerScene;


        private void OnEnable()
        {
            m_SceneRegistration = serializedObject.targetObject as SceneRegistration;

            m_NetworkManagerScene = serializedObject.FindProperty(nameof(SceneRegistration.NetworkManagerScene));

            m_SceneEntryList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(SceneRegistration.SceneRegistrations)), true, true, true, true);
            m_SceneEntryList.multiSelect = true;
            m_SceneEntryList.onAddCallback = AddEntry;
            m_SceneEntryList.onRemoveCallback = RemoveEntry;

            m_SceneEntryList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Scene Entries");

            m_SceneEntryList.elementHeight = (4 * (EditorGUIUtility.singleLineHeight + 5));
            m_SceneEntryList.drawElementCallback = DrawSceneEntryItem;
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {

            serializedObject.Update();


            if (m_NetworkManagerScene != null)
            {

                var value = m_NetworkManagerScene.objectReferenceValue as SceneAsset;
                if (value != null)
                {
                    if (EditorGUILayout.LinkButton($"Open Referencing NetworManager Scene: {value.name}"))
                    {
                        EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(value), OpenSceneMode.Single);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            try
            {
                if (m_SceneEntryList != null)
                {
                    m_SceneEntryList.DoLayoutList();
                }
            }
            catch
            {
            }
            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        private void DrawSceneEntryItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            var sceneEntryItem = m_SceneEntryList.serializedProperty.GetArrayElementAtIndex(index);
            var includeInBuild = sceneEntryItem.FindPropertyRelative(nameof(SceneEntry.IncludeInBuild));
            var sceneEntryItemSceneAsset = sceneEntryItem.FindPropertyRelative(nameof(SceneEntry.Scene));
            var sceneEntryItemAdditiveSceneGroup = sceneEntryItem.FindPropertyRelative(nameof(SceneEntry.AdditiveSceneGroup));

            var labelWidth = 130;
            var xpadding = 2;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), $"Scene Entry - {index}");

            rect.y += EditorGUIUtility.singleLineHeight + 5;

            //Draw include in build property
            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), "Include In Build");
            EditorGUI.PropertyField(new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth - xpadding, EditorGUIUtility.singleLineHeight), includeInBuild, GUIContent.none);

            rect.y += EditorGUIUtility.singleLineHeight + 5;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), "Base Scene");
            EditorGUI.PropertyField(new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth - xpadding, EditorGUIUtility.singleLineHeight), sceneEntryItemSceneAsset, GUIContent.none);


            rect.y += EditorGUIUtility.singleLineHeight + 5;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), "Additive Scenes");
            EditorGUI.PropertyField(new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth - xpadding, EditorGUIUtility.singleLineHeight), sceneEntryItemAdditiveSceneGroup, GUIContent.none);
        }

        private void AddEntry(ReorderableList list)
        {
            var newSceneEntry = new SceneEntry();
            newSceneEntry.IncludeInBuild = true;
            newSceneEntry.AddedToList();
            m_SceneRegistration.SceneRegistrations.Add(newSceneEntry);
        }


        private void RemoveEntry(ReorderableList list)
        {
            var selectedItems = new List<SceneEntry>();

            foreach(var index in list.selectedIndices)
            {
                selectedItems.Add(m_SceneRegistration.SceneRegistrations[index]);
            }

            foreach(var sceneEntry in selectedItems)
            {
                sceneEntry.RemovedFromList();
                m_SceneRegistration.SceneRegistrations.Remove(sceneEntry);

            }
        }
    }
}

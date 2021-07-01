using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            m_SceneEntryList.multiSelect = false;
            m_SceneEntryList.onAddCallback = AddEntry;
            m_SceneEntryList.onRemoveCallback = RemoveEntry;
            m_SceneEntryList.onSelectCallback = SelectedItem;
            m_SceneEntryList.onChangedCallback = ListChanged;

            m_SceneEntryList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Scene Entries");

            m_SceneEntryList.elementHeight = (3 * (EditorGUIUtility.singleLineHeight + 5)) + 10;
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
            var sceneSetupItems = sceneEntryItem.FindPropertyRelative(nameof(SceneEntry.m_SavedSceneSetup));
            var sceneEntry = m_SceneRegistration.SceneRegistrations[index];
            //sceneEntry.RefreshAdditiveScenes();
            var includeInBuild = sceneEntryItem.FindPropertyRelative(nameof(SceneEntry.IncludeInBuild));
            var sceneEntryItemSceneAsset = sceneEntryItem.FindPropertyRelative(nameof(SceneEntry.Scene));
            var sceneAssetLoadMode = sceneEntryItem.FindPropertyRelative(nameof(SceneEntry.Mode));
            var sceneEntryItemAdditiveSceneGroup = sceneEntryItem.FindPropertyRelative(nameof(SceneEntry.AdditiveSceneGroup));

            var labelWidth = 100;
            var currentXPosition = rect.x;

            EditorGUI.LabelField(new Rect(currentXPosition, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), $"Scene Entry - {index}");

            if (sceneEntry != null && sceneEntry.IsNetworkManagerScene)
            {
                GUI.enabled = false;
            }

            //Draw include in build property
            currentXPosition += labelWidth;
            EditorGUI.PropertyField(new Rect(currentXPosition, rect.y, 20, EditorGUIUtility.singleLineHeight), includeInBuild, GUIContent.none);

            if (sceneEntry != null && sceneEntry.IsNetworkManagerScene)
            {
                GUI.enabled = true;
            }

            //Draw include in build label
            currentXPosition += 20;
            EditorGUI.LabelField(new Rect(currentXPosition, rect.y, rect.width - (currentXPosition - rect.x), EditorGUIUtility.singleLineHeight), "Include In Build");

            rect.y += EditorGUIUtility.singleLineHeight + 5;
            currentXPosition = rect.x;

            //Draw base scene label
            EditorGUI.LabelField(new Rect(currentXPosition, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), "Base Scene");
            currentXPosition += labelWidth;


            if (sceneEntry != null && sceneEntry.IsNetworkManagerScene)
            {
                GUI.enabled = false;
            }

            //Draw base scene load mode property
            EditorGUI.PropertyField(new Rect(currentXPosition, rect.y, 75, EditorGUIUtility.singleLineHeight), sceneAssetLoadMode, GUIContent.none);
            currentXPosition += 75;

            //Draw base scene asset property
            EditorGUI.PropertyField(new Rect(currentXPosition, rect.y, rect.width - (currentXPosition - rect.x), EditorGUIUtility.singleLineHeight), sceneEntryItemSceneAsset, GUIContent.none);

            if (sceneEntry != null && sceneEntry.IsNetworkManagerScene)
            {
                GUI.enabled = true;
            }

            rect.y += EditorGUIUtility.singleLineHeight + 5;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), "Additive Scenes");

            if(sceneEntry != null)
            {
                var content = string.Empty;
                if (sceneEntry.m_SavedSceneSetup != null)
                {
                    foreach (var contentVal in sceneEntry.m_SavedSceneSetup)
                    {
                        content += $"{SceneRegistration.GetSceneNameFromPath(contentVal.path)},";
                    }
                    GUI.enabled = false;
                    EditorGUI.TextField(new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, EditorGUIUtility.singleLineHeight), content);
                    GUI.enabled = true;
                }
            }

            //EditorGUI.MultiPropertyField(new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, EditorGUIUtility.singleLineHeight), content.ToArray(), sceneSetupItems);
            //EditorGUI.PropertyField(new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, 100), sceneSetupItems, GUIContent.none);
            //EditorGUI.PropertyField(new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, EditorGUIUtility.singleLineHeight), sceneEntryItemAdditiveSceneGroup, GUIContent.none);
        }

        private Dictionary<SceneAsset, Scene> m_SceneAssetToSceneTable = new Dictionary<SceneAsset, Scene>();

        private void ListChanged(ReorderableList list)
        {
            Debug.Log("Things changed!");
        }

        private void SelectedItem(ReorderableList list)
        {
            //var currentScene = SceneManager.GetActiveScene();
            //if (m_SceneRegistration.SceneRegistrations != null)
            //{
            //    if (list.selectedIndices.Count > 0)
            //    {
            //        var sceneEntry = m_SceneRegistration.SceneRegistrations[list.selectedIndices[0]];
            //        var sceneEntryPath = AssetDatabase.GetAssetPath(sceneEntry.Scene);
            //        if (sceneEntryPath == currentScene.path)
            //        {
            //            SceneHierarchyMonitor.RefreshHierarchy();

            //            var scenesInHierarchy = SceneHierarchyMonitor.CurrentScenesInHierarchy;
            //            if (sceneEntry.AdditiveSceneGroup != null)
            //            {
            //                var additiveScenes = sceneEntry.AdditiveSceneGroup.GetAdditiveSceneAssets();
            //                foreach (var sceneAsset in additiveScenes)
            //                {
            //                    var sceneLoaded = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(sceneAsset), OpenSceneMode.Additive);

            //                    if (!m_SceneAssetToSceneTable.ContainsKey(sceneAsset) && sceneLoaded != null)
            //                    {
            //                        m_SceneAssetToSceneTable.Add(sceneAsset, sceneLoaded);
            //                    }
            //                }

            //                foreach (var sceneAsset in scenesInHierarchy)
            //                {
            //                    if (sceneAsset == sceneEntry.Scene)
            //                    {
            //                        continue;
            //                    }
            //                    var isVisible = (sceneAsset.hideFlags & HideFlags.HideInHierarchy) != HideFlags.HideInHierarchy;
            //                    if (!additiveScenes.Contains(sceneAsset))
            //                    {
            //                        if (m_SceneAssetToSceneTable.ContainsKey(sceneAsset))
            //                        {
            //                            if (EditorSceneManager.CloseScene(m_SceneAssetToSceneTable[sceneAsset], true))
            //                            {
            //                                m_SceneAssetToSceneTable.Remove(sceneAsset);
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
        }


        private void AddEntry(ReorderableList list)
        {
            var newSceneEntry = new SceneEntry();
            newSceneEntry.IncludeInBuild = true;
            newSceneEntry.AddedToList();
            if (m_SceneRegistration.SceneRegistrations == null)
            {
                m_SceneRegistration.SceneRegistrations = new List<SceneEntry>();
            }
            m_SceneRegistration.SceneRegistrations.Add(newSceneEntry);
            m_SceneRegistration.ValidateBuildSettingsScenes();
        }


        private void RemoveEntry(ReorderableList list)
        {
            var selectedItems = new List<SceneEntry>();

            foreach (var index in list.selectedIndices)
            {
                selectedItems.Add(m_SceneRegistration.SceneRegistrations[index]);
            }

            foreach (var sceneEntry in selectedItems)
            {
                sceneEntry.RemovedFromList();
                m_SceneRegistration.SceneRegistrations.Remove(sceneEntry);

            }

            m_SceneRegistration.ValidateBuildSettingsScenes();
        }
    }
}

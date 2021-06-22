using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MLAPI.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneRegistration", menuName = "MLAPI/SceneManagement/SceneRegistration")]
    [Serializable]
    public class SceneRegistration : ScriptableObject
    {
        [SerializeField]
        private List<SceneRegistrationEntry> m_SceneRegistrations;

#if UNITY_EDITOR

        static private Dictionary<string, EditorBuildSettingsScene> s_BuildSettingsSceneLookUpTable = new Dictionary<string, EditorBuildSettingsScene>();


        public static string GetSceneNameFromPath(string scenePath)
        {
            var begin = scenePath.LastIndexOf("/", StringComparison.Ordinal) + 1;
            var end = scenePath.LastIndexOf(".", StringComparison.Ordinal);
            return scenePath.Substring(begin, end - begin);
        }

        private static void BuildLookupTableFromEditorBuildSettings()
        {
            s_BuildSettingsSceneLookUpTable.Clear();
            foreach (var editorBuildSettingsScene in EditorBuildSettings.scenes)
            {
                var sceneName = GetSceneNameFromPath(editorBuildSettingsScene.path);

                if(!s_BuildSettingsSceneLookUpTable.ContainsKey(sceneName))
                {
                    s_BuildSettingsSceneLookUpTable.Add(sceneName, editorBuildSettingsScene);
                }
                else
                {
                    //Error
                }
            }
        }

        internal static void AddOrRemoveSceneAsset(SceneAsset scene, bool addScene)
        {
            if(s_BuildSettingsSceneLookUpTable.Count != EditorBuildSettings.scenes.Length)
            {
                BuildLookupTableFromEditorBuildSettings();
            }

            if(addScene)
            {
                // If the scene does not exist in our local list, then add it and update the build settings
                if(!s_BuildSettingsSceneLookUpTable.ContainsKey(scene.name))
                {
                    s_BuildSettingsSceneLookUpTable.Add(scene.name, new EditorBuildSettingsScene(AssetDatabase.GetAssetPath(scene), true));
                    EditorBuildSettings.scenes = s_BuildSettingsSceneLookUpTable.Values.ToArray();
                }
            }
            else
            {
                // If the scene does exist in our local list, then remove it
                if (s_BuildSettingsSceneLookUpTable.ContainsKey(scene.name))
                {
                    s_BuildSettingsSceneLookUpTable.Remove(scene.name);
                    EditorBuildSettings.scenes = s_BuildSettingsSceneLookUpTable.Values.ToArray();
                }
            }
        }


        private void OnValidate()
        {
            foreach(var sceneRegistrationEntry in m_SceneRegistrations)
            {
                sceneRegistrationEntry.ValidateBuildSettingsScenes();
            }
        }
#endif
        public string GetAllScenesForHash()
        {
            var scenesHashBase = string.Empty;
            foreach(var sceneRegistrationEntry in m_SceneRegistrations)
            {
                scenesHashBase += sceneRegistrationEntry.GetAllScenesForHash();
            }
            return scenesHashBase;
        }
    }
}

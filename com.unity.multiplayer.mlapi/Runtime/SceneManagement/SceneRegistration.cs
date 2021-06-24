using System;
using System.Linq;
using System.Collections.Generic;
using MLAPI.Serialization;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

namespace MLAPI.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneRegistration", menuName = "MLAPI/SceneManagement/SceneRegistration")]
    [Serializable]
    public class SceneRegistration : AssetDependency
    {
        [SerializeField]
        private List<SceneRegistrationEntry> m_SceneRegistrations;

        [HideInInspector]
        [SerializeField]
        private string m_NetworkManagerScene;

#if UNITY_EDITOR
        // Since Unity does not support observable collections there are two ways to approach this:
        // 1.) Make a duplicate list that adjusts itself during OnValidate
        // 2.) Make a customizable property editor that can handle the serialization process (which you will end up with two lists in the end anyway)
        // For this pass, I opted for solution #1
        [HideInInspector]
        [SerializeField]
        private List<SceneRegistrationEntry> m_KnownSceneRegistrations;

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

                if (!s_BuildSettingsSceneLookUpTable.ContainsKey(sceneName))
                {
                    s_BuildSettingsSceneLookUpTable.Add(sceneName, editorBuildSettingsScene);
                }
            }
        }

        internal static void AddOrRemoveSceneAsset(SceneAsset scene, bool addScene)
        {
            if (s_BuildSettingsSceneLookUpTable.Count != EditorBuildSettings.scenes.Length)
            {
                BuildLookupTableFromEditorBuildSettings();
            }

            if (addScene)
            {
                // If the scene does not exist in our local list, then add it and update the build settings
                if (!s_BuildSettingsSceneLookUpTable.ContainsKey(scene.name))
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

        [SerializeField]
        [HideInInspector]
        internal bool AssignedToNetworkManager;

        [SceneReadOnlyProperty]
        [SerializeField]
        internal SceneAsset NetworkManagerScene;

        internal void AssignNetworkManagerScene(bool isAssigned = true)
        {
            AssignedToNetworkManager = isAssigned;
            if (isAssigned)
            {
                var currentScene = SceneManager.GetActiveScene();
                NetworkManagerScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScene.path);
                if(NetworkManagerScene != null)
                {
                    m_NetworkManagerScene = NetworkManagerScene.name;
                    AddOrRemoveSceneAsset(NetworkManagerScene, true);
                }                 
            }
            else
            {
                AddOrRemoveSceneAsset(NetworkManagerScene, false);
                NetworkManagerScene = null;
                m_NetworkManagerScene = string.Empty;
            }
            ValidateBuildSettingsScenes();
        }


        protected override bool OnShouldAssetBeIncluded()
        {
            return AssignedToNetworkManager;
        }

        private void OnValidate()
        {
            foreach (var entry in m_SceneRegistrations)
            {
                if (entry != null)
                {
                    entry.AddDependency(this);
                }
            }

            foreach (var entry in m_KnownSceneRegistrations)
            {
                if (entry != null)
                {
                    if (!m_SceneRegistrations.Contains(entry))
                    {
                        entry.RemoveDependency(this);
                    }
                }
            }

            m_KnownSceneRegistrations.Clear();
            m_KnownSceneRegistrations.AddRange(m_SceneRegistrations);            
            ValidateBuildSettingsScenes();
        }

        /// <summary>
        /// Called to determine if there needs to be any adjustments to the build settings
        /// scenes in build list.
        /// </summary>
        internal void ValidateBuildSettingsScenes()
        {
            //Cycle through all scenes registered and validate the build settings scenes list
            if (m_SceneRegistrations != null)
            {
                foreach (var sceneRegistrationEntry in m_SceneRegistrations)
                {
                    if (sceneRegistrationEntry != null)
                    {
                        sceneRegistrationEntry.ValidateBuildSettingsScenes();
                    }
                }
            }
        }

        /// <summary>
        /// This is the root deciding factor for all checks to determine if assets referenced
        /// within this specific branch of scene asset references should be included.
        /// Note: if there are other SceneRegistration instances assigned to other NetworkManagers
        /// then all or some (depending upon what is included in the other SceneRegistration branches)
        /// will still be included.
        /// </summary>
        /// <returns></returns>
        protected override bool OnIsRootAssetDependency()
        {
            return OnShouldAssetBeIncluded();
        }
#endif
        protected override void OnWriteHashSynchValues(NetworkWriter writer)
        {
            if(m_NetworkManagerScene != null && m_NetworkManagerScene != string.Empty)
            {
                writer.WriteString(m_NetworkManagerScene);
            }

            foreach(var sceneRegistrationEntry in m_SceneRegistrations)
            {
                sceneRegistrationEntry.WriteHashSynchValues(writer);
            }            
        }
    }


}

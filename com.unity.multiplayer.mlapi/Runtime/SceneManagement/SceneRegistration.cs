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
        private List<SceneEntry> m_SceneRegistrations;

        [HideInInspector]
        [SerializeField]
        private string m_NetworkManagerScene;

#if UNITY_EDITOR


        public static string GetSceneNameFromPath(string scenePath)
        {
            var begin = scenePath.LastIndexOf("/", StringComparison.Ordinal) + 1;
            var end = scenePath.LastIndexOf(".", StringComparison.Ordinal);
            return scenePath.Substring(begin, end - begin);
        }

        private static void BuildLookupTableFromEditorBuildSettings()
        {
            foreach (var editorBuildSettingsScene in EditorBuildSettings.scenes)
            {
                var sceneName = GetSceneNameFromPath(editorBuildSettingsScene.path);

                if (!NetworkManager.BuildSettingsSceneLookUpTable.ContainsKey(sceneName))
                {
                    NetworkManager.BuildSettingsSceneLookUpTable.Add(sceneName, editorBuildSettingsScene);
                }
            }
        }

        internal static void SynchronizeScenes()
        {
            var currentScenes = new Dictionary<string, EditorBuildSettingsScene>();
            foreach (var sceneEntry in EditorBuildSettings.scenes)
            {
                currentScenes.Add(GetSceneNameFromPath(sceneEntry.path), sceneEntry);
            }

            foreach (var keyPair in NetworkManager.BuildSettingsSceneLookUpTable)
            {
                if (!currentScenes.ContainsKey(keyPair.Key))
                {
                    currentScenes.Add(keyPair.Key,keyPair.Value);
                }
            }
            EditorBuildSettings.scenes = currentScenes.Values.ToArray();

        }


        internal static void AddOrRemoveSceneAsset(SceneAsset scene, bool addScene)
        {
            if (NetworkManager.BuildSettingsSceneLookUpTable == null)
            {
                NetworkManager.BuildSettingsSceneLookUpTable = new Dictionary<string, EditorBuildSettingsScene>();
            }
            if (NetworkManager.BuildSettingsSceneLookUpTable.Count != EditorBuildSettings.scenes.Length)
            {
                BuildLookupTableFromEditorBuildSettings();
            }

            if (addScene)
            {
                // If the scene does not exist in our local list, then add it and update the build settings
                if (!NetworkManager.BuildSettingsSceneLookUpTable.ContainsKey(scene.name))
                {
                    NetworkManager.BuildSettingsSceneLookUpTable.Add(scene.name, new EditorBuildSettingsScene(AssetDatabase.GetAssetPath(scene), true));
                    SynchronizeScenes();
                    //EditorBuildSettings.scenes = NetworkManager.BuildSettingsSceneLookUpTable.Values.ToArray();
                }
            }
            else
            {
                // If the scene does exist in our local list, then remove it
                if (NetworkManager.BuildSettingsSceneLookUpTable.ContainsKey(scene.name))
                {
                    NetworkManager.BuildSettingsSceneLookUpTable.Remove(scene.name);

                    SynchronizeScenes();
                    //EditorBuildSettings.scenes = NetworkManager.BuildSettingsSceneLookUpTable.Values.ToArray();
                }
            }
        }

        internal bool AssignedToNetworkManager
        {
            get
            {
                if (NetworkManagerScene != null)
                {
                    return true;
                }
                return false;
            }
        }

        [SceneReadOnlyProperty]
        [SerializeField]
        internal SceneAsset NetworkManagerScene;

        internal void AssignNetworkManagerScene(bool isAssigned = true)
        {
            if (isAssigned)
            {
                var currentScene = SceneManager.GetActiveScene();
                NetworkManagerScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScene.path);
                if (NetworkManagerScene != null)
                {
                    m_NetworkManagerScene = NetworkManagerScene.name;
                    AddOrRemoveSceneAsset(NetworkManagerScene, true);
                }
            }
            else
            {
                if (NetworkManagerScene != null)
                {
                    AddOrRemoveSceneAsset(NetworkManagerScene, false);
                }
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
            //if (!BuildPipeline.isBuildingPlayer)
            {
                ValidateBuildSettingsScenes();
            }
        }

        /// <summary>
        /// Called to determine if there needs to be any adjustments to the build settings
        /// scenes in build list.
        /// </summary>
        internal void ValidateBuildSettingsScenes()
        {
            //Cycle through all scenes registered and validate the build settings scenes list
            if (m_SceneRegistrations != null && m_SceneRegistrations.Count > 0)
            {
                var shouldInclude = ShouldAssetBeIncluded();
                var partOfRootBranch = BelongsToRootAssetBranch();

                foreach (var sceneRegistrationEntry in m_SceneRegistrations)
                {
                    if (sceneRegistrationEntry != null && sceneRegistrationEntry.Scene != null)
                    {
                        if (sceneRegistrationEntry.SceneEntryName != sceneRegistrationEntry.Scene.name)
                        {
                            sceneRegistrationEntry.SceneEntryName = sceneRegistrationEntry.Scene.name;
                        }

                        AddOrRemoveSceneAsset(sceneRegistrationEntry.Scene, shouldInclude && partOfRootBranch && sceneRegistrationEntry.IncludeInBuild);

                        sceneRegistrationEntry.UpdateAdditiveSceneGroup(this);
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
            if (m_NetworkManagerScene != null && m_NetworkManagerScene != string.Empty)
            {
                writer.WriteString(m_NetworkManagerScene);
            }

            foreach (var sceneRegistrationEntry in m_SceneRegistrations)
            {
                if (sceneRegistrationEntry != null && sceneRegistrationEntry.SceneEntryName != null && sceneRegistrationEntry.SceneEntryName != string.Empty)
                {
                    writer.WriteString(sceneRegistrationEntry.SceneEntryName);
                    if (sceneRegistrationEntry.AdditiveSceneGroup != null)
                    {
                        sceneRegistrationEntry.AdditiveSceneGroup.WriteHashSynchValues(writer);
                    }
                }
            }
        }

        public List<string> GetAllScenes()
        {
            var allScenes = new List<string>();

            if (m_NetworkManagerScene != null && m_NetworkManagerScene != string.Empty)
            {
                allScenes.Add(m_NetworkManagerScene);
            }

            foreach (var sceneRegistrationEntry in m_SceneRegistrations)
            {
                if (sceneRegistrationEntry != null && sceneRegistrationEntry.SceneEntryName != null && sceneRegistrationEntry.SceneEntryName != string.Empty)
                {
                    allScenes.Add(sceneRegistrationEntry.SceneEntryName);
                }
            }

            return allScenes;
        }
    }

    /// <summary>
    /// A container class to hold the editor specific assets and
    /// the scene name that it is pointing to for runtime
    /// </summary>
    [Serializable]
    public class SceneEntry : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        public SceneAsset Scene;

        [Tooltip("When set to true, this will automatically register all of the additive scenes with the build settings scenes in build list.  If false, then the scene(s) have to be manually added or will not be included in the build.")]
        public bool IncludeInBuild;

        [SerializeField]
        [HideInInspector]
        private AddtiveSceneGroup m_KnownAdditiveSceneGroup;

        internal void UpdateAdditiveSceneGroup(SceneRegistration assetDependency)
        {
            if (AdditiveSceneGroup != m_KnownAdditiveSceneGroup)
            {
                if (m_KnownAdditiveSceneGroup != null)
                {
                    m_KnownAdditiveSceneGroup.RemoveDependency(assetDependency);
                    m_KnownAdditiveSceneGroup.ValidateBuildSettingsScenes();
                }
            }

            if (AdditiveSceneGroup != null)
            {
                AdditiveSceneGroup.AddDependency(assetDependency);
                AdditiveSceneGroup.ValidateBuildSettingsScenes();
            }


            if (m_KnownAdditiveSceneGroup != AdditiveSceneGroup)
            {
                m_KnownAdditiveSceneGroup = AdditiveSceneGroup;
            }
        }
#endif

        public AddtiveSceneGroup AdditiveSceneGroup;


        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (Scene != null && SceneEntryName != Scene.name)
            {
                SceneEntryName = Scene.name;
            }
#endif
        }
        [HideInInspector]
        public string SceneEntryName;


    }


}

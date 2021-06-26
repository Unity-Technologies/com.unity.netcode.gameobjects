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
                if (!NetworkManager.BuildSettingsSceneLookUpTable.ContainsKey(editorBuildSettingsScene.path))
                {
                    NetworkManager.BuildSettingsSceneLookUpTable.Add(editorBuildSettingsScene.path, editorBuildSettingsScene);
                }
            }
        }

        /// <summary>
        /// This is needed in the event you have multiple assembly definitions that all have SceneRegistrations within them
        /// We have to make sure that we use the scene path as the key in order to allow for "the same scene name" to exist
        /// but in a different path.  As such, when we are synchronizing our build settings scenes in build list we need to
        /// always do a full comparison against the existing scenes in build list and our current assembly's scenes in build
        /// list.
        /// </summary>
        /// <param name="removeEntry">path to sceneAsset that will be excluding from build settings scenes in build list</param>
        internal static void SynchronizeScenes(string removeEntry = null)
        {
            var currentScenes = new Dictionary<string, EditorBuildSettingsScene>();
            foreach (var sceneEntry in EditorBuildSettings.scenes)
            {
                if (removeEntry != null && sceneEntry.path == removeEntry)
                {
                    continue;
                }
                if (!currentScenes.ContainsKey(sceneEntry.path))
                {
                    currentScenes.Add(sceneEntry.path, sceneEntry);
                }
                else
                {
                    Debug.LogWarning($"{sceneEntry.path} already exists in dictionary!");
                }
            }

            foreach (var keyPair in NetworkManager.BuildSettingsSceneLookUpTable)
            {
                if (!currentScenes.ContainsKey(keyPair.Key))
                {
                    currentScenes.Add(keyPair.Key, keyPair.Value);
                }
            }
            currentScenes = currentScenes.OrderBy(x => x.Key).ToDictionary((keyItem)=>keyItem.Key,(valueItem) => valueItem.Value);

            EditorBuildSettings.scenes = currentScenes.Values.ToArray();
        }

        /// <summary>
        /// Adds or removes a scene asset to the build settings scenes in build list
        /// </summary>
        /// <param name="scene">SceneAsset</param>
        /// <param name="addScene">true or false</param>
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

            var assetPath = AssetDatabase.GetAssetPath(scene);

            if (addScene)
            {
                // If the scene does not exist in our local list, then add it and update the build settings
                if (!NetworkManager.BuildSettingsSceneLookUpTable.ContainsKey(assetPath))
                {
                    NetworkManager.BuildSettingsSceneLookUpTable.Add(assetPath, new EditorBuildSettingsScene(assetPath, true));
                    SynchronizeScenes();
                }
            }
            else
            {
                // If the scene does exist in our local list, then remove it
                if (NetworkManager.BuildSettingsSceneLookUpTable.ContainsKey(assetPath))
                {
                    NetworkManager.BuildSettingsSceneLookUpTable.Remove(assetPath);
                    SynchronizeScenes(assetPath);
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

        /// <summary>
        /// For this asset dependency, we check to see if we have been added to a NetworkManager instance within a scene
        /// that is contained within the project
        /// </summary>
        /// <returns>true or false</returns>
        protected override bool OnShouldAssetBeIncluded()
        {
            return AssignedToNetworkManager;
        }

        private void OnValidate()
        {
            ValidateBuildSettingsScenes();
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
        /// <summary>
        /// Invoked to generate the hash value for the NetworkConfig comparison when a client is connecting
        /// </summary>
        /// <param name="writer"></param>
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

        /// <summary>
        /// Gets all scene names within this scene registration's scope
        /// NOTE: Scene names can be the same and this is not a good way to distinguish between scenes but is
        /// used for backwards compatibility purposes until no longer needed
        /// </summary>
        /// <returns></returns>
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
    public class SceneEntry : SceneEntryBsase
    {
#if UNITY_EDITOR
        [SerializeField]
        [HideInInspector]
        private AddtiveSceneGroup m_KnownAdditiveSceneGroup;

        /// <summary>
        /// Updates the dependencies for the additive scene group associated with this SceneEntry
        /// </summary>
        /// <param name="assetDependency"></param>
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
                if (IncludeInBuild)
                {
                    AdditiveSceneGroup.AddDependency(assetDependency);
                }
                else
                {
                    AdditiveSceneGroup.RemoveDependency(assetDependency);
                }
                AdditiveSceneGroup.ValidateBuildSettingsScenes();
            }

            if (m_KnownAdditiveSceneGroup != AdditiveSceneGroup)
            {
                m_KnownAdditiveSceneGroup = AdditiveSceneGroup;
            }
        }
#endif
        public AddtiveSceneGroup AdditiveSceneGroup;
    }


    /// <summary>
    /// A container class to hold the editor specific assets and
    /// the scene name that it is pointing to for runtime
    /// </summary>
    [Serializable]
    public class SceneEntryBsase : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [Tooltip("When set to true, this will automatically register all of the additive scenes (including groups) with the build settings scenes in build list.  If false, then the scene(s) have to be manually added or will not be included in the build.")]
        public bool IncludeInBuild;
        public SceneAsset Scene;
#endif

        [HideInInspector]
        public string SceneEntryName;

        public void OnAfterDeserialize()
        {
        }

        /// <summary>
        /// This is used to extract the scene name from the SceneAsset
        /// </summary>
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (Scene != null && SceneEntryName != Scene.name)
            {
                SceneEntryName = Scene.name;
            }
#endif
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Unity.Netcode
{
    /// <summary>
    /// This replaces the need to register scenes and contains the Scenes in Build list (as strings)
    /// Scenes are ordered identically to the Scenes in Build list indices values
    /// For refined control over which scenes can be loaded or unloaded during a netcode game session,
    /// use the <see cref="NetworkSceneManager.VerifySceneBeforeLoading"/> event to add additional
    /// constraints over which scenes are considered valid.
    /// In order for clients to get this notification you must subscribe to the <see cref="NetworkSceneManager.OnSceneVerificationFailed"/> event.
    /// </summary>
    public class ScenesInBuild : ScriptableObject
    {
        //[HideInInspector]
        [SerializeField]
        internal List<string> Scenes;

#if UNITY_INCLUDE_TESTS

        /// <summary>
        /// Determines if we are running a unit test
        /// In DEVELOPMENT_BUILD we only check for the InitTestScene
        /// </summary>
        private static bool IsRunningUnitTest()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                return false;
            }
#endif

            var isTesting = SceneManager.GetActiveScene().name.StartsWith("InitTestScene");


            // For scenes in build, we have to check whether we are running a unit test or not each time
            return isTesting;
        }

        /// <summary>
        /// Assures the ScenesInBuild asset exists and if in the editor that it is always up to date
        /// </summary>
        internal static void SynchronizeOrCreate(NetworkManager networkManager)
        {
            var isUnitTestRunning = IsRunningUnitTest();

#if UNITY_EDITOR
            // If we are testing or we are playing (in editor) and ScenesInBuild is null then we want to initialize and populate the ScenesInBuild asset.
            // Otherwise, there are special edge case scenarios where we might want to repopulate this list
            // The scenario with EditorApplication.isPlaying and ScenesInBuild being null is where we loaded a scene that did not have a NetworkManager but
            // we transition to a scene with a NetworkManager while playing in the editor.  Under this condition we have to assign and populate.
            if ( (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying && !EditorApplication.isUpdating) || isUnitTestRunning ||
                ( (networkManager.ScenesInBuild == null || networkManager.ScenesInBuild != null && networkManager.ScenesInBuild.Scenes.Count == 0) && EditorApplication.isPlaying) )
            {
                if (networkManager.ScenesInBuild == null)
                {
                    networkManager.ScenesInBuild = InitializeScenesInBuild(networkManager);
                }

                networkManager.ScenesInBuild.PopulateScenesInBuild();
            }
#else

            // Only if we are running a stand alone build as for unit or integration testing
            if (isUnitTestRunning)
            {
                if (networkManager.ScenesInBuild == null)
                {
                    networkManager.ScenesInBuild = CreateInstance<ScenesInBuild>();
                }
                var currentlyActiveSceneName = SceneManager.GetActiveScene().name;

                // If the unit test scene is not already added to the ScenesInBuild
                if(!networkManager.ScenesInBuild.Scenes.Contains(currentlyActiveSceneName))
                {
                    // Then add it into the valid scenes that it can be loaded
                    networkManager.ScenesInBuild.Scenes.Add(currentlyActiveSceneName);
                }
            }
#endif
        }


#if UNITY_EDITOR

        private bool m_CheckHasBeenApplied;

        /// <summary>
        /// This will add a play mode state change watch to determine when we are done with a unit test
        /// in order to re-synchronize our ScenesInBuild asset.
        /// </summary>
        private void CheckForEndOfUnitTest()
        {
            if (!m_CheckHasBeenApplied)
            {
                EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
                m_CheckHasBeenApplied = true;
            }
        }

        /// <summary>
        /// Check for when we enter into editor mode so we can remove any scenes that might have been added to the
        /// scenes list.
        /// </summary>
        private void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
                m_CheckHasBeenApplied = false;
                PopulateScenesInBuild();
            }
        }

        /// <summary>
        /// This will create a new ScenesInBuildList asset if one does not exist and will adjust the path to the ScenesInBuildList
        /// asset if the asset is moved.  This will also notify the user if more than one ScenesInBuildList asset exists.
        /// </summary>
        /// <param name="networkManager">The relative network manager instance</param>
        /// <returns></returns>
        internal static ScenesInBuild InitializeScenesInBuild(NetworkManager networkManager)
        {
            var foundScenesInBuildList = AssetDatabase.FindAssets("ScenesInBuildList");
            if (foundScenesInBuildList.Length > 0)
            {
                if (foundScenesInBuildList.Length > 1)
                {
                    var message = "There are multiple instances of your ScenesInBuildList:\n";

                    foreach (var entry in foundScenesInBuildList)
                    {
                        message += $"{AssetDatabase.GUIDToAssetPath(entry)}\n";
                    }
                    message += "Using first entry.  Please remove one of the instances if that is not the right asset path!";
                    Debug.LogError(message);
                }
                networkManager.DefaultScenesInBuildAssetNameAndPath = AssetDatabase.GUIDToAssetPath(foundScenesInBuildList[0]);
            }
            var scenesInBuild = (ScenesInBuild)AssetDatabase.LoadAssetAtPath(networkManager.DefaultScenesInBuildAssetNameAndPath, typeof(ScenesInBuild));
            if (scenesInBuild == null)
            {
                scenesInBuild = CreateInstance<ScenesInBuild>();
                AssetDatabase.CreateAsset(scenesInBuild, networkManager.DefaultScenesInBuildAssetNameAndPath);
            }
            return scenesInBuild;
        }

        /// <summary>
        /// Populates the scenes from the Scenes in Build list.
        /// If testing, then this is ignored (i.e. some tests require loading of scenes not in the Scenes in Build list)
        /// </summary>
        internal void PopulateScenesInBuild()
        {
            var shouldRebuild = false;
            var isTesting = IsRunningUnitTest();

            // if we have no scenes registered or we have changed the scenes in the build we should rebuild
            if (Scenes == null)
            {
                shouldRebuild = true;
            }
            else if (!isTesting)
            {
                if (EditorBuildSettings.scenes.Length != Scenes.Count)
                {
                    shouldRebuild = true;
                }
                else
                {
                    // Verify our order hasn't changed and if it has then we should rebuild
                    for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                    {
                        var scene = EditorBuildSettings.scenes[i];
                        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                        // We could either have changed the order or we could have rename something or we could have removed and added
                        // either case we rebuild if they don't align properly
                        if (Scenes[i] != sceneAsset.name)
                        {
                            shouldRebuild = true;
                            break;
                        }
                    }
                }
            }
            else
            if ( Scenes.Count != SceneManager.sceneCountInBuildSettings)
            {
                shouldRebuild = true;
            }

            if (shouldRebuild)
            {
                Scenes = new List<string>();
                if (!isTesting)
                {
                    // Normal scenes in build list generation
                    for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                    {
                        var scene = EditorBuildSettings.scenes[i];
                        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                        Scenes.Add(sceneAsset.name);
                    }
                }
                else
                {
                    CheckForEndOfUnitTest();

                    // This is only for unit or integration testing
                    for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                    {
                        // First we make sure we order everything exactly as it is in the build settings
                        if (EditorBuildSettings.scenes.Length > i)
                        {
                            var scene = EditorBuildSettings.scenes[i];
                            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                            Scenes.Add(sceneAsset.name);
                        }
                        else // If we are testing then we will just add the remaining scenes into our registered scenes list
                        if (!Scenes.Contains(SceneManager.GetSceneByBuildIndex(i).name))
                        {
                            Scenes.Add(SceneManager.GetSceneByBuildIndex(i).name);
                        }
                    }
                }
            }

            if (!isTesting)
            {
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Depending upon the current editor state or if we are running a unit test or not, this will refresh the Scenes in Build list
        /// </summary>
        private void OnValidate()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying && !EditorApplication.isUpdating || IsRunningUnitTest())
            {
                PopulateScenesInBuild();
            }
        }

#endif // UNITY_EDITOR
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

    }
}

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


        [HideInInspector]
        [SerializeField]
        internal List<string> Scenes;


        private bool IsRunningUnitTest()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                return false;
            }
#endif
            // For scenes in build, we have to check whether we are running a unit test or not each time
            return SceneManager.GetActiveScene().name.StartsWith("InitTestScene");
        }


#if UNITY_EDITOR

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
            var isRunningUnitTest = IsRunningUnitTest();
            if (Scenes != null && Scenes.Count > 0 && isRunningUnitTest)
            {
                return;
            }
            Scenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {

                if (isRunningUnitTest && i >= EditorBuildSettings.scenes.Length)
                {
                    break;
                }
                var scene = EditorBuildSettings.scenes[i];
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                Scenes.Add(sceneAsset.name);
            }
            if (!isRunningUnitTest)
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
#endif
    }
}

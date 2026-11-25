using Unity.Netcode.Logging;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_7000_0_OR_NEWER
using UnityEditor.Build.Content;
#endif

namespace Unity.Netcode.GameObjects.Editor
{
    /// <summary>
    /// A build scene callback that sets the <see cref
    /// "NetworkObject.InScenePlaced"/> property to true for all <see cref="NetworkObject"/>s in the scene.
    /// Ensures that InScenePlaced is always true for all objects in the scene.
    /// </summary>
    /// <remarks>
    /// This will always run as a scene is loaded in playmode or while processed for a player build.
    /// </remarks>
#if UNITY_7000_0_OR_NEWER
    internal class SetInScenePlaced : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        public override int GetPostprocessOrder() => 0;

        void OnProcessScene(Scene scene, SceneImportContext sceneImportContext)
        {
            InScenePlaceChecks.ReportSceneIssues(scene, sceneImportContext.awakeDidRun);
        }
    }
#else
    internal class SetInScenePlaced : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            InScenePlaceChecks.ReportSceneIssues(scene, Application.isPlaying);
        }
    }
#endif

    internal static class InScenePlaceChecks
    {
        public static void ReportSceneIssues(Scene scene, bool hasAwakeRun)
        {
            var log = new ContextualLogger();
            log.AddInfo(scene.name, scene.handle);
            foreach (var networkObject in FindObjects.FromSceneByType<NetworkObject>(scene, true))
            {
                // Trap for users just creating things during runtime where this will be zero.
                if (networkObject.GlobalObjectIdHash == 0)
                {
                    log.Warning(new Context(LogLevel.Developer, $"{nameof(NetworkObject)}'s GlobalObjectIdHash value is zero! Runtime creating of {nameof(NetworkObject)}s is not supported. Skipping processing.").AddNetworkObject(networkObject));
                    continue;
                }
                if (networkObject.SceneOrigin.IsValid() && networkObject.SceneOrigin.handle != scene.handle)
                {
                    log.Warning(new Context(LogLevel.Developer, $"{nameof(NetworkObject)}'s SceneOrigin doesn't match current scene being processed! Skipping processing.").AddInfo("SceneOrigin", networkObject.SceneOriginHandle).AddNetworkObject(networkObject));
                    continue;
                }

                if (networkObject.HasBeenSpawned)
                {
                    log.Error(new Context(LogLevel.Normal, $"Processing {nameof(NetworkObject)} that has already been spawned! This should not be possible. Skipping processing.").AddNetworkObject(networkObject));
                    continue;
                }

                // If already marked, do nothing.
                if (networkObject.InScenePlaced)
                {
                    continue;
                }

                networkObject.InScenePlaced = true;
                // Mark that awake has run so this could have been a dynamically instantiated object.
                networkObject.InScenePlacedPostProcessorMarkedDuringRuntime = hasAwakeRun;
            }
        }
    }

    /// <summary>
    /// Gives in-scene placed <see cref="NetworkObject"/>s the GlobalObjectIdHash they missed, when their scene is saved.
    /// </summary>
    /// <remarks>
    /// NetworkObject.OnValidate can only generate the hash once the scene has an asset path, and a later
    /// revalidation of a plain scene object doesn't mark the scene dirty. So a NetworkObject added to a new scene
    /// (by an editor script, for instance) and saved with it keeps a hash of 0, and never spawns. After each save,
    /// this regenerates any hash that is still 0 and saves the scene again.
    /// </remarks>
    [InitializeOnLoad]
    internal static class InSceneNetworkObjectIdRepair
    {
        private static bool s_Saving;

        static InSceneNetworkObjectIdRepair()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        /// <summary>Regenerates the hash of each NetworkObject in the scene that has none; returns how many now have one.</summary>
        internal static int Repair(Scene scene)
        {
            var repaired = 0;
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
            {
                return repaired;
            }
            foreach (var networkObject in FindObjects.FromSceneByType<NetworkObject>(scene, true))
            {
                if (networkObject.GlobalObjectIdHash != 0)
                {
                    continue;
                }
                networkObject.OnValidate();
                if (networkObject.GlobalObjectIdHash != 0)
                {
                    repaired++;
                }
            }
            return repaired;
        }

        private static void OnSceneSaved(Scene scene)
        {
            if (s_Saving || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (Repair(scene) == 0)
            {
                return;
            }
            s_Saving = true;
            try
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                s_Saving = false;
            }
        }
    }

    /// <summary>
    /// An <see cref="AssetPostprocessor"/> that sets the <see cref="NetworkObject.InScenePlaced"/> property to false for all <see cref="NetworkObject"/>s in prefabs.
    /// Ensures that InScenePlaced is always false for all prefab objects.
    /// This is important because when a prefab is instantiated in the scene, it should be treated as a dynamically spawned object.
    /// </summary>
    internal class InScenePlacedPrefabBuilder : AssetPostprocessor
    {
        public void OnPostprocessPrefab(GameObject root)
        {
            var networkObjects = root.GetComponentsInChildren<NetworkObject>(true);
            foreach (var networkObject in networkObjects)
            {
                networkObject.InScenePlaced = false;
            }
        }
    }
}

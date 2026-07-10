using Unity.Netcode.Logging;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// A <see cref="IProcessSceneWithReport"/> that sets the <see cref
    /// "NetworkObject.InScenePlaced"/> property to true for all <see cref="NetworkObject"/>s in the scene.
    /// Ensures that InScenePlaced is always true for all objects in the scene.
    /// </summary>
    /// <remarks>
    /// This will always run as the game enters the scene,
    /// </remarks>
    internal class SetInScenePlaced : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var log = new ContextualLogger();
            log.AddInfo(scene.name, scene.handle);
            foreach (var networkObject in FindObjects.FromSceneByType<NetworkObject>(scene, true))
            {
                if (networkObject.SceneOrigin.handle != scene.handle)
                {
                    log.Warning(new Context(LogLevel.Developer, $"{nameof(NetworkObject)}'s SceneOrigin doesn't match current scene being processed! Skipping processing.").AddInfo("SceneOrigin", networkObject.SceneOriginHandle).AddNetworkObject(networkObject));
                    continue;
                }

                if (networkObject.HasBeenSpawned)
                {
                    log.Error(new Context(LogLevel.Normal, $"Processing {nameof(NetworkObject)} that has already been spawned! This should not be possible. Skipping processing.").AddNetworkObject(networkObject));
                    continue;
                }

                networkObject.InScenePlaced = true;
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

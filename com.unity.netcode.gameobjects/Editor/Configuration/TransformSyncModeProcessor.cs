using Unity.Netcode.GameObjects.Editor.Configuration;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// Applies the project wide <see cref="NetcodeForGameObjectsProjectSettings.TransformSyncMode"/> to the
    /// <see cref="NetworkConfig"/>.
    /// </summary>
    /// <remarks>
    /// This runs both when entering play mode and while building, and operates on the scene being processed as
    /// opposed to the authored asset, so it never dirties a user's scene.
    /// </remarks>
    internal class SetTransformSyncMode : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var transformSyncMode = NetcodeForGameObjectsProjectSettings.instance.TransformSyncMode;
            foreach (var networkManager in FindObjects.FromSceneByType<NetworkManager>(scene, true))
            {
                if (networkManager.NetworkConfig == null)
                {
                    continue;
                }
                networkManager.NetworkConfig.TransformSyncMode = transformSyncMode;
            }
        }
    }

    /// <summary>
    /// Applies the project wide <see cref="NetcodeForGameObjectsProjectSettings.TransformSyncMode"/> to any
    /// <see cref="NetworkManager"/> within a prefab as the prefab will be is imported.
    /// </summary>
    /// <remarks>
    /// Covers projects that instantiate their <see cref="NetworkManager"/> from a prefab as opposed to placing
    /// it in a scene.
    /// </remarks>
    internal class TransformSyncModePrefabProcessor : AssetPostprocessor
    {
        public void OnPostprocessPrefab(GameObject root)
        {
            var networkManagers = root.GetComponentsInChildren<NetworkManager>(true);
            if (networkManagers.Length == 0)
            {
                return;
            }

            var transformSyncMode = NetcodeForGameObjectsProjectSettings.instance.TransformSyncMode;
            foreach (var networkManager in networkManagers)
            {
                if (networkManager.NetworkConfig == null)
                {
                    continue;
                }
                networkManager.NetworkConfig.TransformSyncMode = transformSyncMode;
            }
        }
    }
}

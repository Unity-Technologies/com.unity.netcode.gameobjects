using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor.Configuration
{
    /// <summary>
    /// Updates a <see cref="NetworkPrefabs"/> instance when prefabs are updated (created, moved, deleted) in the project.
    /// </summary>
    public class NetworkPrefabProcessor : AssetPostprocessor
    {
        public const string DefaultNetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
        private static NetworkPrefabs s_PrefabsList;

        // Unfortunately this method is required by the asset pipeline to be static
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (s_PrefabsList == null)
            {
                s_PrefabsList = GetOrCreateNetworkPrefabs(DefaultNetworkPrefabsPath);
            }

            bool markDirty = false;
            foreach (var assetPath in importedAssets)
            {
                // We only care about GameObjects, skip everything else. Can't use the more targeted
                // OnPostProcessPrefabs since that's not called for moves or deletes
                if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(GameObject))
                {
                    continue;
                }

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (go.TryGetComponent<NetworkObject>(out _))
                {
                    s_PrefabsList.Add(new NetworkPrefab { Prefab = go });
                    markDirty = true;
                }
            }

            if (markDirty)
            {
                EditorUtility.SetDirty(s_PrefabsList);
            }

            // TODO: Handle delete & moved
        }

        private static NetworkPrefabs GetOrCreateNetworkPrefabs(string path)
        {
            var defaultPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabs>(path);
            if (defaultPrefabs == null)
            {
                defaultPrefabs = ScriptableObject.CreateInstance<NetworkPrefabs>();
                defaultPrefabs.IsDefault = true;
                AssetDatabase.CreateAsset(defaultPrefabs, path);

                // TODO: Process entire project and do first populate
            }

            return defaultPrefabs;
        }
    }
}

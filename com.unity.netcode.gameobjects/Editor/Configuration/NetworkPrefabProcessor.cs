using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor.Configuration
{
    /// <summary>
    /// Updates the default <see cref="NetworkPrefabsList"/> instance when prefabs are updated (created, moved, deleted) in the project.
    /// </summary>
    public class NetworkPrefabProcessor : AssetPostprocessor
    {
        public const string DefaultNetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
        private static NetworkPrefabsList s_PrefabsList;

        // Unfortunately this method is required by the asset pipeline to be static
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var settings = NetcodeForGameObjectsSettings.GetOrCreateSettings();
            if (!settings.GenerateDefaultNetworkPrefabs)
            {
                return;
            }

            bool ProcessImportedAssets(string[] importedAssets1)
            {
                var dirty = false;
                foreach (var assetPath in importedAssets1)
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
                        s_PrefabsList.List.Add(new NetworkPrefab { Prefab = go });
                        dirty = true;
                    }
                }

                return dirty;
            }

            bool ProcessDeletedAssets(string[] strings)
            {
                var dirty = false;
                List<string> deleted = new List<string>(strings);
                for (int i = s_PrefabsList.List.Count - 1; i >= 0 && deleted.Count > 0; --i)
                {
                    string noPath = AssetDatabase.GetAssetPath(s_PrefabsList.List[i].Prefab);
                    for (int j = strings.Length - 1; j >= 0; --j)
                    {
                        if (noPath == strings[j])
                        {
                            s_PrefabsList.List.RemoveAt(i);
                            deleted.RemoveAt(j);
                            dirty = true;
                        }
                    }
                }

                return dirty;
            }

            if (s_PrefabsList == null)
            {
                s_PrefabsList = GetOrCreateNetworkPrefabs(DefaultNetworkPrefabsPath, out var newList);
                // A new list already processed all existing assets, no need to double-process imports & deletes
                if (newList)
                {
                    return;
                }
            }

            var markDirty = ProcessImportedAssets(importedAssets);
            markDirty &= ProcessDeletedAssets(deletedAssets);

            if (markDirty)
            {
                EditorUtility.SetDirty(s_PrefabsList);
            }
        }

        private static NetworkPrefabsList GetOrCreateNetworkPrefabs(string path, out bool isNew)
        {
            var defaultPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(path);
            if (defaultPrefabs == null)
            {
                isNew = true;
                defaultPrefabs = ScriptableObject.CreateInstance<NetworkPrefabsList>();
                defaultPrefabs.IsDefault = true;
                AssetDatabase.CreateAsset(defaultPrefabs, path);

                // This could be very expensive in large projects... maybe make it manually triggered via a menu?
                defaultPrefabs.List = FindAll();
                EditorUtility.SetDirty(defaultPrefabs);
                AssetDatabase.SaveAssetIfDirty(defaultPrefabs);
            }

            isNew = false;
            return defaultPrefabs;
        }

        private static List<NetworkPrefab> FindAll()
        {
            List<NetworkPrefab> list = new();

            string[] guids = AssetDatabase.FindAssets("t:GameObject");
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (go.TryGetComponent(out NetworkObject _))
                {
                    list.Add(new NetworkPrefab { Prefab = go });
                }
            }

            return list;
        }
    }
}
#if UNIFIED_NETCODE
using Unity.NetCode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode.GameObjects.Editor.Configuration
{
    /// <summary>
    /// Keeps the project's <see cref="NetCodeConfig"/> aligned with NGO needs whenever the project is running in
    /// hybrid mode (N4E installed and at least one registered NGO network prefab has a GhostObject component).
    /// </summary>
    /// <remarks>
    /// This does not create <see cref="NetCodeConfig"/>. This finds the one N4E created and modifies it.
    /// </remarks>
    internal static class HybridNetcodeConfigApplier
    {
        [InitializeOnLoadMethod]
        private static void OnApplicationStart()
        {
            // Cross-assembly ordering between the two is not a documented contract.
            // Defer rather than racing it.
            EditorApplication.delayCall += OnDelayCall;

            // A NetworkManager in an unopened scene is not loaded, so its tick rate cannot be read at this point.
            // Rescan when a scene opens to pick it up.
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnDelayCall()
        {
            EditorApplication.delayCall -= OnDelayCall;
            Apply(false);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Apply(false);
        }

        /// <summary>
        /// Adjusts <see cref="NetCodeConfig"/> for NGO hybrid mode.
        /// </summary>
        /// <param name="applyRecommended">
        /// Driven by the button in Project Settings:
        /// - When true: it re-applies the full tuned set even if this project has already had it applied once.
        /// - When false: default NGO settings are only written once, the first time they are applied. From that
        /// point forward, the user's edits are not overwritten.
        /// </param>
        internal static void Apply(bool applyRecommended)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var config = ResolveGlobalConfig();
            if (config == null || !IsHybridProject())
            {
                return;
            }

            var settings = NetcodeForGameObjectsProjectSettings.instance;
            var isFirstApply = settings.HybridDefaultsVersion < HybridNetcodeDefaults.Version;
            var changed = false;

            if (applyRecommended || isFirstApply)
            {
                changed = HybridNetcodeDefaults.ApplyRecommended(config, ResolveTickRate(config));
                if (changed)
                {
                    Debug.Log($"[Netcode] Applied the NGO hybrid mode defaults to '{config.name}'. These are tuned for NGO and can be changed freely; they will not be re-applied automatically. Use Project Settings > Multiplayer > Netcode for GameObjects to restore them.", config);
                }

                // Recorded even when the config already matched and nothing was written. Leaving it unrecorded would
                // make the next domain reload a first application again, which would revert the user's next edit.
                settings.HybridDefaultsVersion = HybridNetcodeDefaults.Version;
                settings.SaveSettings();
            }
            else
            {
                // Outside the one-shot, only the settings hybrid mode cannot run without are enforced, plus the tick
                // rate, which NGO owns.
                changed = HybridNetcodeDefaults.ApplyRequired(config);
                if (changed)
                {
                    Debug.LogWarning($"[Netcode] Corrected required hybrid mode settings on '{config.name}'. Netcode for GameObjects owns world creation and requires single world hosting, so these two cannot be changed while ghost prefabs are registered.", config);
                }

                changed |= HybridNetcodeDefaults.ApplyTickRate(config, ResolveTickRate(config));
            }

            if (!changed)
            {
                return;
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
        }

        /// <summary>
        /// True when a registered network prefab carries a ghost.
        /// </summary>
        /// <remarks>
        /// The <see cref="NetworkPrefabsList"/> assets are scanned rather than the loaded <see cref="NetworkManager"/>s
        /// because a manager living in an unopened scene is not loaded and would not be found. A ghost prefab sitting
        /// in a list is treated as intent to run hybrid mode even if no manager references that list yet.
        /// </remarks>
        internal static bool IsHybridProject()
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(NetworkPrefabsList)}"))
            {
                var prefabsList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(AssetDatabase.GUIDToAssetPath(guid));
                if (HasGhost(prefabsList))
                {
                    return true;
                }
            }

            // Prefabs added directly to a NetworkManager never reach a list asset, so the loaded managers still have
            // to be checked.
            foreach (var networkManager in Resources.FindObjectsOfTypeAll<NetworkManager>())
            {
                if (HasGhost(networkManager))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when this <see cref="NetworkManager"/> registers a prefab carrying a ghost, either directly or through
        /// one of its assigned <see cref="NetworkPrefabsList"/> assets.
        /// </summary>
        /// <param name="networkManager">The manager to inspect.</param>
        /// <returns>Whether this manager takes part in hybrid mode.</returns>
        private static bool HasGhost(NetworkManager networkManager)
        {
            var prefabs = networkManager == null ? null : networkManager.NetworkConfig?.Prefabs;
            if (prefabs == null)
            {
                return false;
            }

            foreach (var prefab in prefabs.Prefabs)
            {
                if (HasGhost(prefab))
                {
                    return true;
                }
            }

            foreach (var prefabsList in prefabs.NetworkPrefabsLists)
            {
                if (HasGhost(prefabsList))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when this <see cref="NetworkPrefabsList"/> holds a prefab carrying a ghost.
        /// </summary>
        /// <param name="prefabsList">The list to inspect.</param>
        /// <returns>Whether this list takes part in hybrid mode.</returns>
        internal static bool HasGhost(NetworkPrefabsList prefabsList)
        {
            if (prefabsList == null)
            {
                return false;
            }

            foreach (var prefab in prefabsList.PrefabList)
            {
                if (HasGhost(prefab))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasGhost(NetworkPrefab prefab)
        {
            return prefab?.Prefab != null
                && prefab.Prefab.TryGetComponent<NetworkObject>(out var networkObject)
                && networkObject.HasGhost;
        }

        /// <summary>
        /// Resolves the config N4E considers global, falling back to a project scan when N4E has not assigned one yet.
        /// </summary>
        /// <returns>The config to adjust or null if no config exists.</returns>
        internal static NetCodeConfig ResolveGlobalConfig()
        {
            if (NetCodeConfig.Global != null)
            {
                return NetCodeConfig.Global;
            }

            var guids = AssetDatabase.FindAssets($"t:{nameof(NetCodeConfig)}");
            return guids.Length == 1 ? AssetDatabase.LoadAssetAtPath<NetCodeConfig>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }

        /// <summary>
        /// Returns either the current N4E tick rate or the NGO <see cref="NetworkConfig.TickRate"/>.
        /// If no NetworkManager taking part in hybrid mode is loaded, it returns N4E's tick rate.
        /// If one is loaded, then it returns NGO's tick rate.
        /// </summary>
        /// <remarks>
        /// Only managers registering a ghost prefab are considered. A conventional NGO manager running at a different
        /// tick rate has no bearing on the interval N4E should synchronize ghosts at.
        /// </remarks>
        /// <param name="config">The config, used as the fallback when no NetworkManager can be found.</param>
        /// <returns>The tick rate to write into the config.</returns>
        private static uint ResolveTickRate(NetCodeConfig config)
        {
            var found = 0u;
            var diverged = false;
            foreach (var networkManager in Resources.FindObjectsOfTypeAll<NetworkManager>())
            {
                if (!HasGhost(networkManager))
                {
                    continue;
                }

                var tickRate = networkManager.NetworkConfig?.TickRate ?? 0u;
                if (tickRate == 0)
                {
                    continue;
                }

                diverged |= found != 0 && found != tickRate;
                found = tickRate;
            }

            if (diverged)
            {
                Debug.LogWarning($"[Netcode] Found hybrid mode {nameof(NetworkManager)}s with differing {nameof(NetworkConfig.TickRate)} values. '{config.name}' has been set to {found}; hybrid mode expects a single tick rate across the network prefabs carrying a ghost.", config);
            }

            // Nothing to read from (a prefab-only project, one mid-import, or the manager's scene is not open yet)
            // leaves the config as it is. Opening that scene runs this again.
            return found != 0 ? found : (uint)config.ClientServerTickRate.SimulationTickRate;
        }
    }

    /// <summary>
    /// Re-runs the hybrid config pass when an import could have turned this into a hybrid project.
    /// </summary>
    /// <remarks>
    /// Both a prefab gaining a GhostObject and a prefab list gaining an existing ghost prefab reach hybrid mode, so
    /// both imports are watched.
    /// </remarks>
    internal class HybridNetcodeConfigPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var assetPath in importedAssets)
            {
                if (ImportReachesHybridMode(assetPath))
                {
                    HybridNetcodeConfigApplier.Apply(false);
                    return;
                }
            }
        }

        /// <summary>
        /// Cheap check for whether an imported asset could have introduced a ghost, so that the full project scan in
        /// <see cref="HybridNetcodeConfigApplier.Apply"/> is only paid when it might matter.
        /// </summary>
        /// <param name="assetPath">The imported asset.</param>
        /// <returns>Whether the import is worth a rescan.</returns>
        private static bool ImportReachesHybridMode(string assetPath)
        {
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetType == typeof(GameObject))
            {
                var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                return gameObject != null && gameObject.TryGetComponent<NetworkObject>(out var networkObject) && networkObject.HasGhost;
            }

            if (assetType == typeof(NetworkPrefabsList))
            {
                return HybridNetcodeConfigApplier.HasGhost(AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(assetPath));
            }

            return false;
        }
    }
}
#endif

#if UNIFIED_NETCODE
using Unity.NetCode;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.GameObjects.Editor.Configuration
{
    /// <summary>
    /// Keeps the project's <see cref="NetCodeConfig"/> aligned with what NGO needs whenever the project is running in
    /// hybrid mode (Netcode for Entities installed, and at least one registered network prefab carrying a ghost).
    /// </summary>
    /// <remarks>
    /// This never creates a <see cref="NetCodeConfig"/>. Netcode for Entities already creates one unconditionally from
    /// its own <see cref="InitializeOnLoadMethodAttribute"/>, and creating a second lands the project in N4E's
    /// multiple-config error path. We find the one N4E settled on and correct it.
    /// </remarks>
    internal static class HybridNetcodeConfigApplier
    {
        [InitializeOnLoadMethod]
        private static void OnApplicationStart()
        {
            // N4E creates and assigns the global config from its own InitializeOnLoadMethod. Cross-assembly ordering
            // between the two is not a documented contract, so defer rather than racing it.
            EditorApplication.delayCall += OnDelayCall;
        }

        private static void OnDelayCall()
        {
            EditorApplication.delayCall -= OnDelayCall;
            Apply(false);
        }

        /// <summary>
        /// Corrects the global <see cref="NetCodeConfig"/> for hybrid mode, if this is a hybrid project.
        /// </summary>
        /// <param name="applyRecommended">
        /// When true, re-applies the full tuned set even if this project has already had it applied once. Driven by the
        /// button in Project Settings. When false, the tuned values are only written the first time, so that a user's
        /// own edits are not repeatedly overwritten.
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

            settings.HybridDefaultsVersion = HybridNetcodeDefaults.Version;
            settings.SaveSettings();

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
        }

        /// <summary>
        /// True when any <see cref="NetworkManager"/> in the project has a registered prefab carrying a ghost.
        /// </summary>
        /// <returns>Whether this project is configured for hybrid mode.</returns>
        internal static bool IsHybridProject()
        {
            foreach (var networkManager in Resources.FindObjectsOfTypeAll<NetworkManager>())
            {
                var prefabs = networkManager.NetworkConfig?.Prefabs;
                if (prefabs == null)
                {
                    continue;
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
                    if (prefabsList == null)
                    {
                        continue;
                    }

                    foreach (var prefab in prefabsList.PrefabList)
                    {
                        if (HasGhost(prefab))
                        {
                            return true;
                        }
                    }
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
        /// <returns>The config to correct, or null when none exists yet.</returns>
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
        /// The tick rate N4E should be driven at. NGO owns this, so it comes from <see cref="NetworkConfig.TickRate"/>.
        /// </summary>
        /// <param name="config">The config, used as the fallback when no NetworkManager can be found.</param>
        /// <returns>The tick rate to write into the config.</returns>
        private static uint ResolveTickRate(NetCodeConfig config)
        {
            var found = 0u;
            var diverged = false;
            foreach (var networkManager in Resources.FindObjectsOfTypeAll<NetworkManager>())
            {
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
                Debug.LogWarning($"[Netcode] Found {nameof(NetworkManager)}s with differing {nameof(NetworkConfig.TickRate)} values. '{config.name}' has been set to {found}; hybrid mode expects a single tick rate across the project.", config);
            }

            // No NetworkManager to read from (a prefab-only project, or one mid-import) leaves the config as it is.
            return found != 0 ? found : (uint)config.ClientServerTickRate.SimulationTickRate;
        }
    }

    /// <summary>
    /// Re-runs the hybrid config pass when a prefab import could have turned this into a hybrid project.
    /// </summary>
    internal class HybridNetcodeConfigPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var assetPath in importedAssets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(GameObject))
                {
                    continue;
                }

                var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (gameObject != null && gameObject.TryGetComponent<NetworkObject>(out var networkObject) && networkObject.HasGhost)
                {
                    HybridNetcodeConfigApplier.Apply(false);
                    return;
                }
            }
        }
    }
}
#endif

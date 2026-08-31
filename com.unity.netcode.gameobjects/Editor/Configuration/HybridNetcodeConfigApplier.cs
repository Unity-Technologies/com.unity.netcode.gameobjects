#if UNIFIED_NETCODE
using Unity.NetCode;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.GameObjects.Editor.Configuration
{
    /// <summary>
    /// Writes the <see cref="NetCodeConfig"/> values NGO recommends for hybrid mode, once, the first time a
    /// <see cref="NetCodeConfig"/> is available.
    /// </summary>
    /// <remarks>
    /// This does not create <see cref="NetCodeConfig"/>. This finds the one N4E created and modifies it.
    /// Nothing tracks the project after that write. The defaults are inert in a project with no hybrid prefabs, and
    /// <see cref="NetworkManager"/> re-aligns the tick rate at start-up in a project that has them, so there is no
    /// reason to scan for ghost prefabs from the editor.
    /// </remarks>
    internal static class HybridNetcodeConfigApplier
    {
        /// <summary>
        /// Whether the user has to opt into the experimental unified netcode API before NGO writes anything.
        /// </summary>
        /// <remarks>
        /// TODO-RELEASE: Set this to true before the 6000.7.0 release manifest submission if Netcode for Entities
        /// ships the unified API as experimental and its scripting defines. 
        /// Note: This is deliberately not a const: IDE0035 (remove unreachable code) is an error in this repository, so a
        /// const would fail the standards job as soon as it was set to false.
        /// </remarks>
        internal static readonly bool RequiresExperimentalOptIn = false;

        private static NetCodeConfig s_ScannedConfig;
        private static bool s_ConfigScanned;

        [InitializeOnLoadMethod]
        private static void OnApplicationStart()
        {
            // Cross-assembly ordering between the two is not a documented contract.
            // Defer rather than racing it.
            EditorApplication.delayCall += OnDelayCall;
        }

        private static void OnDelayCall()
        {
            EditorApplication.delayCall -= OnDelayCall;
            ApplyDefaults(false);
        }

        /// <summary>
        /// Writes the NGO hybrid mode defaults into the project's <see cref="NetCodeConfig"/>.
        /// </summary>
        /// <param name="force">
        /// Driven by the button in Project Settings:
        /// - When true: re-applies the full tuned set even though this project has already had it applied once.
        /// - When false: writes only if this project has never had them written. From that point forward, the user's
        /// edits are not overwritten.
        /// </param>
        internal static void ApplyDefaults(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var settings = NetcodeForGameObjectsProjectSettings.instance;
            if (RequiresExperimentalOptIn && !settings.EnableUnifiedNetcodeApi)
            {
                return;
            }

            if (!force && settings.HybridDefaultsVersion >= HybridNetcodeDefaults.Version)
            {
                return;
            }

            // A project with no config yet leaves the marker unrecorded so that the next domain reload tries again.
            // N4E creates one on any domain reload that finds none.
            var config = ResolveGlobalConfig();
            if (config == null)
            {
                return;
            }

            if (HybridNetcodeDefaults.ApplyRecommended(config, HybridNetcodeDefaults.DefaultTickRate))
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssetIfDirty(config);
                Debug.Log($"[Netcode] Applied the NGO hybrid mode defaults to '{config.name}'. These are tuned for NGO and can be changed freely; they will not be re-applied automatically. Use Project Settings > Multiplayer > Netcode for GameObjects to restore them.", config);
            }

            // Recorded even when the config already matched and nothing was written. Leaving it unrecorded would make
            // the next domain reload a first application again, which would revert the user's next edit.
            settings.HybridDefaultsVersion = HybridNetcodeDefaults.Version;
            settings.SaveSettings();
        }

        /// <summary>
        /// Resolves the config N4E considers global, falling back to a project scan when N4E has not assigned one yet.
        /// </summary>
        /// <remarks>
        /// The scan is done at most once per domain reload, including when it finds nothing, because this is also
        /// reached from OnGUI and <see cref="AssetDatabase.FindAssets"/> walks the entire project. A config created
        /// after the scan is picked up on the next domain reload.
        /// </remarks>
        /// <returns>The config to adjust or null if no config exists.</returns>
        internal static NetCodeConfig ResolveGlobalConfig()
        {
            if (NetCodeConfig.Global != null)
            {
                return NetCodeConfig.Global;
            }

            if (!s_ConfigScanned)
            {
                s_ConfigScanned = true;
                var guids = AssetDatabase.FindAssets($"t:{nameof(NetCodeConfig)}");
                s_ScannedConfig = guids.Length == 1 ? AssetDatabase.LoadAssetAtPath<NetCodeConfig>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
            }

            return s_ScannedConfig;
        }
    }
}
#endif

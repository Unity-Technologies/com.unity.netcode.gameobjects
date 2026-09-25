#if UNIFIED_NETCODE
using Unity.Netcode.Logging;
#if !UNIFIED_NETCODE_7_0_0
using NetcodeConfig = Unity.NetCode.NetCodeConfig;
#endif
using UnityEditor;

namespace Unity.Netcode.GameObjects.Editor.Configuration
{
    /// <summary>
    /// Writes the <see cref="NetcodeConfig"/> values NGO recommends for hybrid mode, once, the first time a
    /// <see cref="NetcodeConfig"/> is available.
    /// </summary>
    /// <remarks>
    /// This does not create <see cref="NetcodeConfig"/>. This finds the one N4E created and modifies it.
    /// Nothing tracks the project after that write. The defaults are inert in a project with no hybrid prefabs, and
    /// <see cref="NetworkManager"/> re-aligns the tick rate at start-up in a project that has them, so there is no
    /// reason to scan for ghost prefabs from the editor.
    /// </remarks>
    internal static class HybridNetcodeConfigApplier
    {
        [InitializeOnLoadMethod]
        private static void OnApplicationStart()
        {
            // N4E assigns NetcodeConfig.Global from its own [InitializeOnLoadMethod] and creates the asset when
            // there is none. delayCall runs after those have completed, which is what makes Global reliable here
            // without a lookup of our own.
            EditorApplication.delayCall += OnDelayCall;
        }

        private static void OnDelayCall()
        {
            EditorApplication.delayCall -= OnDelayCall;
            ApplyDefaults(false);
        }

        /// <summary>
        /// Writes the NGO hybrid mode defaults into the project's <see cref="NetcodeConfig"/>.
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
            if (!force && settings.HybridDefaultsVersion >= HybridNetcodeDefaults.Version)
            {
                return;
            }

            // A project with no config yet leaves the marker unrecorded so that the next domain reload tries again.
            // N4E creates one on any domain reload that finds none.
            var config = NetcodeConfig.Global;
            if (config == null)
            {
                return;
            }

            if (HybridNetcodeDefaults.ApplyRecommended(config, HybridNetcodeDefaults.DefaultTickRate))
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssetIfDirty(config);
                new ContextualLogger(config).Info(new Context(LogLevel.Developer, $"Applied the hybrid mode defaults to '{config.name}'. These are tuned for Netcode for GameObjects and can be changed freely; they will not be re-applied automatically. Use Project Settings > Multiplayer > Netcode for GameObjects to restore them.").AddTag("Unified"));
            }

            // Recorded even when the config already matched and nothing was written. Leaving it unrecorded would make
            // the next domain reload a first application again, which would revert the user's next edit.
            settings.HybridDefaultsVersion = HybridNetcodeDefaults.Version;
            settings.SaveSettings();
        }
    }
}
#endif

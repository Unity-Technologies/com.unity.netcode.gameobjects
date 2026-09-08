using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Netcode.GameObjects.Editor.Configuration
{
    /// <summary>
    /// A <see cref="ScriptableSingleton{T}"/> of type <see cref="NetcodeForGameObjectsProjectSettings"/>.
    /// </summary>
    [FilePath("ProjectSettings/NetcodeForGameObjects.asset", FilePathAttribute.Location.ProjectFolder)]
    [MovedFrom(true, "Unity.Netcode.Editor.Configuration", "Unity.Netcode.Editor", null)]
    public class NetcodeForGameObjectsProjectSettings : ScriptableSingleton<NetcodeForGameObjectsProjectSettings>
    {
        internal static readonly string DefaultNetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
        /// <summary>
        /// The path and name for the DefaultNetworkPrefabs asset.
        /// </summary>
        [SerializeField] public string NetworkPrefabsPath = DefaultNetworkPrefabsPath;

        /// <summary>
        /// A temporary network prefabs path used internally.
        /// </summary>
        public string TempNetworkPrefabsPath;

        private void OnEnable()
        {
            if (NetworkPrefabsPath.Length == 0)
            {
                NetworkPrefabsPath = DefaultNetworkPrefabsPath;
            }
            TempNetworkPrefabsPath = NetworkPrefabsPath;
        }

        /// <summary>
        /// Used to determine whether the default network prefabs asset should be generated or not.
        /// </summary>
        [SerializeField]
        public bool GenerateDefaultNetworkPrefabs = true;

#if UNIFIED_NETCODE
        /// <summary>
        /// Whether the user has opted into the experimental unified netcode API.
        /// </summary>
        /// <remarks>
        /// Only consulted while <see cref="HybridNetcodeConfigApplier.RequiresExperimentalOptIn"/> holds. Turning it
        /// off again hides the hybrid section and leaves the NetCodeConfig exactly as it is; the marker below is what
        /// keeps turning it back on from overwriting anything.
        /// </remarks>
        [SerializeField]
        public bool EnableUnifiedNetcodeApi;

        /// <summary>
        /// The hybrid mode default values already applied to this project's NetCodeConfig.
        /// </summary>
        /// <remarks>
        /// Zero means they have never been applied. Persisting this value is what keeps the tuned values a one-shot.
        /// For users who deliberately change them, they are not overwritten on the next domain reload.
        /// </remarks>
        [SerializeField]
        public int HybridDefaultsVersion;
#endif

        internal void SaveSettings()
        {
            Save(true);
        }
    }
}

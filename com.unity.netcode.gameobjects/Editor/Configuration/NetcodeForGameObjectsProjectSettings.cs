using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.GameObjects.Editor.Configuration
{
    /// <summary>
    /// A <see cref="ScriptableSingleton{T}"/> of type <see cref="NetcodeForGameObjectsProjectSettings"/>.
    /// </summary>
    [FilePath("ProjectSettings/NetcodeForGameObjects.asset", FilePathAttribute.Location.ProjectFolder)]
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
        /// The version of the NGO hybrid mode defaults already applied to this project's NetCodeConfig.
        /// </summary>
        /// <remarks>
        /// Zero means they have never been applied. Recording it is what keeps the tuned values a one-shot, so that a
        /// user who deliberately changes them does not have them overwritten on the next domain reload.
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

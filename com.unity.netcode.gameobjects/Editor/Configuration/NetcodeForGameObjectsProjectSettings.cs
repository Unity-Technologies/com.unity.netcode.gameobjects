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
        /// The version of the hybrid mode default values already written into this project's NetcodeConfig, or zero
        /// when they have never been written.
        /// </summary>
        /// <remarks>
        /// A version rather than a flag so a later revision of those values re-applies exactly once. Recording it is
        /// what keeps them a one-shot: a user who changes them is not overwritten on the next domain reload.
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

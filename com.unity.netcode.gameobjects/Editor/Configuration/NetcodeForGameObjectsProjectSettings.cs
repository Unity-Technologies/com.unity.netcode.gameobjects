using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor.Configuration
{
    /// <summary>
    /// Project settings for Netcode for GameObjects.
    /// </summary>
    [FilePath("ProjectSettings/NetcodeForGameObjects.asset", FilePathAttribute.Location.ProjectFolder)]
    public class NetcodeForGameObjectsProjectSettings : ScriptableSingleton<NetcodeForGameObjectsProjectSettings>
    {
        /// <summary>
        /// The default path for network prefabs.
        /// </summary>
        internal static readonly string DefaultNetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";

        /// <summary>
        /// The path to the network prefabs.
        /// </summary>
        [SerializeField] public string NetworkPrefabsPath = DefaultNetworkPrefabsPath;

        /// <summary>
        /// A temporary path to the network prefabs.
        /// </summary>
        public string TempNetworkPrefabsPath;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        private void OnEnable()
        {
            if (NetworkPrefabsPath == "")
            {
                NetworkPrefabsPath = DefaultNetworkPrefabsPath;
            }
            TempNetworkPrefabsPath = NetworkPrefabsPath;
        }

        /// <summary>
        /// Indicates whether to generate default network prefabs.
        /// </summary>
        [SerializeField]
        public bool GenerateDefaultNetworkPrefabs = true;

        /// <summary>
        /// Saves the project settings.
        /// </summary>
        internal void SaveSettings()
        {
            Save(true);
        }
    }
}

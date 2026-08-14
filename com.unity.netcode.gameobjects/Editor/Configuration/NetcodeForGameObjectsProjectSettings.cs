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

        /// <summary>
        /// The project wide <see cref="TransformSyncModes"/> that is applied to <see cref="NetworkConfig.TransformSyncMode"/>.
        /// </summary>
        /// <remarks>
        /// The two modes are not wire compatible with one another, so this is authored once for the project as
        /// opposed to per <see cref="NetworkManager"/>.
        /// </remarks>
        [SerializeField]
        public TransformSyncModes TransformSyncMode = TransformSyncModes.PerInstance;

        internal void SaveSettings()
        {
            Save(true);
        }
    }
}

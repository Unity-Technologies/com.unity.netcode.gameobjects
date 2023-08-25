using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor.Configuration
{
    [FilePath("ProjectSettings/NetcodeForGameObjects.asset", FilePathAttribute.Location.ProjectFolder)]
    public class NetcodeForGameObjectsProjectSettings : ScriptableSingleton<NetcodeForGameObjectsProjectSettings>
    {
        internal static readonly string DefaultNetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
        [SerializeField] public string NetworkPrefabsPath = DefaultNetworkPrefabsPath;
        public string TempNetworkPrefabsPath;

        private void OnEnable()
        {
            if (NetworkPrefabsPath == "")
            {
                NetworkPrefabsPath = DefaultNetworkPrefabsPath;
            }
            TempNetworkPrefabsPath = NetworkPrefabsPath;
        }

        [SerializeField]
        public bool GenerateDefaultNetworkPrefabs = true;

        internal void SaveSettings()
        {
            Save(true);
        }
    }
}

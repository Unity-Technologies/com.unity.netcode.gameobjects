using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;


namespace Unity.Netcode.Editor.Configuration
{
    internal class NetcodeForGameObjectsEditorSettings
    {
        internal const string AutoAddNetworkObjectIfNoneExists = "AutoAdd-NetworkObject-When-None-Exist";
        internal const string InstallMultiplayerToolsTipDismissedPlayerPrefKey = "Netcode_Tip_InstallMPTools_Dismissed";

        internal static int GetNetcodeInstallMultiplayerToolTips()
        {
            if (EditorPrefs.HasKey(InstallMultiplayerToolsTipDismissedPlayerPrefKey))
            {
                return EditorPrefs.GetInt(InstallMultiplayerToolsTipDismissedPlayerPrefKey);
            }

            return 0;
        }

        internal static void SetNetcodeInstallMultiplayerToolTips(int toolTipPrefSetting)
        {
            EditorPrefs.SetInt(InstallMultiplayerToolsTipDismissedPlayerPrefKey, toolTipPrefSetting);
        }

        internal static bool GetAutoAddNetworkObjectSetting()
        {
            if (EditorPrefs.HasKey(AutoAddNetworkObjectIfNoneExists))
            {
                return EditorPrefs.GetBool(AutoAddNetworkObjectIfNoneExists);
            }

            return false;
        }

        internal static void SetAutoAddNetworkObjectSetting(bool autoAddSetting)
        {
            EditorPrefs.SetBool(AutoAddNetworkObjectIfNoneExists, autoAddSetting);
        }
    }

    [FilePath("ProjectSettings/NetcodeForGameObjects.settings", FilePathAttribute.Location.ProjectFolder)]
    internal class NetcodeForGameObjectsProjectSettings : ScriptableSingleton<NetcodeForGameObjectsProjectSettings>
    {
        [SerializeField]
        [FormerlySerializedAs("GenerateDefaultNetworkPrefabs")]
        private byte m_GenerateDefaultNetworkPrefabs;

        public bool GenerateDefaultNetworkPrefabs
        {
            get
            {
                return m_GenerateDefaultNetworkPrefabs != 0;
            }
            set
            {
                m_GenerateDefaultNetworkPrefabs = (byte)(value ? 1 : 0);
            }
        }

        internal void SaveSettings()
        {
            Save(true);
        }
    }
}

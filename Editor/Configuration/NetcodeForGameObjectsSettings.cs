using UnityEditor;
using UnityEngine;


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
        [SerializeField] public bool GenerateDefaultNetworkPrefabs = true;

        internal void SaveSettings()
        {
            Save(true);
        }
    }
}

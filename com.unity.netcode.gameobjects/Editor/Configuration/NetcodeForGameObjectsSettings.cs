using System;
using UnityEditor;
using UnityEngine;


namespace Unity.Netcode.Editor.Configuration
{
    internal class NetcodeForGameObjectsSettings
    {
        internal const string AutoAddNetworkObjectIfNoneExists = "AutoAdd-NetworkObject-When-None-Exist";
        internal const string InstallMultiplayerToolsTipDismissedPlayerPrefKey = "Netcode_Tip_InstallMPTools_Dismissed";

        private const string k_SettingsPath = "ProjectSettings/NetcodeForGameObjectsSettings.json";
        private static NetcodeForGameObjectsSettings s_Instance;

        [SerializeField]
        public bool GenerateDefaultNetworkPrefabs = true;

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

        public static NetcodeForGameObjectsSettings GetOrCreateSettings()
        {
            NetcodeForGameObjectsSettings LoadFromFile()
            {
                if (!System.IO.File.Exists(k_SettingsPath))
                {
                    return null;
                }

                // Load from file
                try
                {
                    var text = System.IO.File.ReadAllText(k_SettingsPath);
                    return JsonUtility.FromJson<NetcodeForGameObjectsSettings>(text);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return null;
                }
            }

            // Load or instantiate a new instance
            s_Instance ??= LoadFromFile() ?? new NetcodeForGameObjectsSettings();
            return s_Instance;
        }

        public void Save(string path = k_SettingsPath)
        {
            path ??= k_SettingsPath;
            var json = JsonUtility.ToJson(this, true);
            System.IO.File.WriteAllText(path, json);
        }
    }
}

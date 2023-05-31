using System;
using System.IO;
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

    [Serializable]
    internal class NetcodeForGameObjectsJsonProjectSettings
    {
        private const string FilePath = "ProjectSettings/NetcodeForGameObjects.settings";

        private static NetcodeForGameObjectsJsonProjectSettings s_Instance;

        public static NetcodeForGameObjectsJsonProjectSettings Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    if (File.Exists(FilePath))
                    {
                        var json = File.ReadAllText(FilePath);
                        if (json == "" || json[0] != '{')
                        {
                            s_Instance = new NetcodeForGameObjectsJsonProjectSettings();
                        }
                        else
                        {
                            s_Instance = JsonUtility.FromJson<NetcodeForGameObjectsJsonProjectSettings>(json);
                            s_Instance.OnLoad();
                        }
                    }
                }

                return s_Instance;
            }
        }

        internal static readonly string DefaultNetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
        public string NetworkPrefabsPath = DefaultNetworkPrefabsPath;

        [NonSerialized]
        public string TempNetworkPrefabsPath;

        public bool GenerateDefaultNetworkPrefabs;

        private void OnLoad()
        {
            if (NetworkPrefabsPath == "")
            {
                NetworkPrefabsPath = DefaultNetworkPrefabsPath;
            }
            TempNetworkPrefabsPath = NetworkPrefabsPath;
        }

        internal void SaveSettings()
        {
            var json = JsonUtility.ToJson(this);
            File.WriteAllText(FilePath, json);
        }
    }
}

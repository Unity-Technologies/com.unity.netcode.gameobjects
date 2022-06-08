using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor.Configuration
{
    internal static class NetworkPrefabSettingsProvider
    {
        private const float k_MaxLabelWidth = 450f;
        private static float s_MaxLabelWidth;

        [SettingsProvider]
        public static SettingsProvider CreateNetworkPrefabSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Project/NetcodeForGameObjects", SettingsScope.Project)
            {
                label = "Netcode for GameObjects",
                keywords = new [] { "netcode", "editor" },
                guiHandler = OnGuiHandler
            };

            return provider;
        }

        private static void OnGuiHandler(string obj)
        {
            const string generateNetworkPrefabsString = "Generate Network Prefabs List";

            if (s_MaxLabelWidth == 0)
            {
                s_MaxLabelWidth = EditorStyles.label.CalcSize(new GUIContent(generateNetworkPrefabsString)).x;
                s_MaxLabelWidth = Mathf.Min(k_MaxLabelWidth, s_MaxLabelWidth);
            }
            EditorGUIUtility.labelWidth = s_MaxLabelWidth;

            var settings = NetcodeForGameObjectsSettings.GetOrCreateSettings();

            GUILayout.Label("Network Prefabs", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var generateDefaultPrefabs = EditorGUILayout.Toggle(
                new GUIContent(
                    generateNetworkPrefabsString,
                    "When enabled, a default NetworkPrefabsList object will be added to your project and kept up " +
                    "to date with all NetworkObject prefabs."),
                settings.GenerateDefaultNetworkPrefabs,
                GUILayout.Width(s_MaxLabelWidth + 20));
            if (EditorGUI.EndChangeCheck())
            {
                settings.GenerateDefaultNetworkPrefabs = generateDefaultPrefabs;
                settings.Save();
            }
        }
    }

    [Serializable]
    internal class NetcodeForGameObjectsSettings
    {
        private const string k_SettingsPath = "ProjectSettings/NetcodeForGameObjectsSettings.json";
        private static NetcodeForGameObjectsSettings s_Instance;

        [SerializeField]
        public bool GenerateDefaultNetworkPrefabs = true;

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

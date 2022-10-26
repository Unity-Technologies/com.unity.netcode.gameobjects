using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor.Configuration
{
    internal static class NetcodeSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateNetcodeSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Project/NetcodeForGameObjects", SettingsScope.Project)
            {
                label = "Netcode for GameObjects",
                keywords = new[] { "netcode", "editor" },
                guiHandler = OnGuiHandler,
            };

            return provider;
        }

        internal static NetcodeSettingsLabel NetworkObjectsSectionLabel = new NetcodeSettingsLabel("NetworkObjects");

        internal static NetcodeSettingsToggle AutoAddNetworkObjectToggle = new NetcodeSettingsToggle("Auto-Add NetworkObjects", "When enabled, Netcode for GameObjects " +
            "will automatically add a NetworkObject to a GameObject that does not already have one.", 20);

        private static void OnGuiHandler(string obj)
        {
            var autoAddNetworkObjectSetting = NetcodeForGameObjectsSettings.GetAutoAddNetworkObjectSetting();
            EditorGUI.BeginChangeCheck();
            NetworkObjectsSectionLabel.DrawLabel();
            autoAddNetworkObjectSetting = AutoAddNetworkObjectToggle.DrawToggle(autoAddNetworkObjectSetting);
            if (EditorGUI.EndChangeCheck())
            {
                NetcodeForGameObjectsSettings.SetAutoAddNetworkObjectSetting(autoAddNetworkObjectSetting);
            }
        }
    }

    internal class NetcodeSettingsLabel : NetcodeGuiSetings
    {
        private string m_LabelContent;

        public void DrawLabel()
        {
            GUILayout.Label(m_LabelContent, EditorStyles.boldLabel, m_LayoutWidth);
        }

        public NetcodeSettingsLabel(string labelText, float layoutOffset = 0.0f)
        {
            m_LabelContent = labelText;
            AdjustLableSize(labelText, layoutOffset);
        }
    }

    internal class NetcodeSettingsToggle : NetcodeGuiSetings
    {
        private GUIContent m_ToggleContent;

        private int m_LayoutOffset;

        public bool DrawToggle(bool currentSetting)
        {
            return EditorGUILayout.Toggle(m_ToggleContent, currentSetting, m_LayoutWidth);
        }

        public NetcodeSettingsToggle(string labelText, string toolTip, float layoutOffset)
        {
            AdjustLableSize(labelText, layoutOffset);
            m_ToggleContent = new GUIContent(labelText, toolTip);
        }
    }


    internal class NetcodeGuiSetings
    {
        private const float k_MaxLabelWidth = 450f;
        protected GUILayoutOption m_LayoutWidth { get; private set; }

        protected void AdjustLableSize(string labelText, float offset = 0.0f)
        {
            var layoutWidth = Mathf.Min(k_MaxLabelWidth, EditorStyles.label.CalcSize(new GUIContent(labelText)).x);
            m_LayoutWidth = GUILayout.Width(layoutWidth + offset);
        }
    }

}

#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && MULTIPLAYER_TOOLS

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Netcode.Transports.UTP
{
    [RequireComponent(typeof(UnityTransport))]
    public class NetworkConditioning : MonoBehaviour
    {
        public string ConnectionTypeString;

        public int PacketDelayMs;

        public int PacketJitterMs;

        public int PacketLossPercent;

        public NetworkConditioningPreset ConnectionType
        {
            get
            {
                if (string.IsNullOrEmpty(ConnectionTypeString))
                {
                    return NetworkConditioningPreset.ConnectionTypePresets.First();
                }

                if (ConnectionTypeString == NetworkConditioningPreset.CustomPresetName)
                {
                    return new NetworkConditioningPreset(
                        ConnectionTypeString,
                        string.Empty,
                        PacketDelayMs,
                        PacketJitterMs,
                        PacketLossPercent);
                }

                return NetworkConditioningPreset.ConnectionTypePresets.First(x => x.Name == ConnectionTypeString);
            }
        }

        void Start()
        {
            UpdateLiveParameters();
        }

        public void UpdateLiveParameters()
        {
            var transport = GetComponent<UnityTransport>();
            transport.RefreshSimulationPipelineParameters(ConnectionType);
        }
    }

    [CustomEditor(typeof(NetworkConditioning))]
    public class NetworkConditioningEditor : Editor
    {
        const string UXML = "Packages/com.unity.netcode.gameobjects/Runtime/Transports/UTP/NetworkConditioningEditor.uxml";

        NetworkConditioning m_NetworkConditioning;

        VisualElement m_Inspector;
        DropdownField PresetDropdown => m_Inspector.Q<DropdownField>(nameof(PresetDropdown));
        SliderInt PacketDelaySlider => m_Inspector.Q<SliderInt>(nameof(PacketDelaySlider));
        IntegerField PacketDelayField => m_Inspector.Q<IntegerField>(nameof(PacketDelayField));
        SliderInt PacketJitterSlider => m_Inspector.Q<SliderInt>(nameof(PacketJitterSlider));
        IntegerField PacketJitterField => m_Inspector.Q<IntegerField>(nameof(PacketJitterField));
        SliderInt PacketLossSlider => m_Inspector.Q<SliderInt>(nameof(PacketLossSlider));
        IntegerField PacketLossField => m_Inspector.Q<IntegerField>(nameof(PacketLossField));

        public override VisualElement CreateInspectorGUI()
        {
            m_NetworkConditioning = target as NetworkConditioning;
            m_Inspector = new VisualElement();

            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(m_Inspector);

            PresetDropdown.choices = NetworkConditioningPreset.ConnectionTypePresets.Select(x => x.Name).ToList();
            PresetDropdown.index = PresetDropdown.choices.IndexOf(m_NetworkConditioning.ConnectionType.Name);
            PresetDropdown.RegisterCallback<ChangeEvent<string>>(OnPresetSelected);

            PacketDelaySlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketDelay(change.newValue));
            PacketDelayField.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketDelay(change.newValue));
            PacketJitterSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketJitter(change.newValue));
            PacketJitterField.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketJitter(change.newValue));
            PacketLossSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketLoss(change.newValue));
            PacketLossField.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketLoss(change.newValue));

            UpdatePacketDelay(m_NetworkConditioning.ConnectionType.PacketDelayMs);
            UpdatePacketJitter(m_NetworkConditioning.ConnectionType.PacketJitterMs);
            UpdatePacketLoss(m_NetworkConditioning.ConnectionType.PacketLossPercent);

            UpdateEnabled();

            return m_Inspector;
        }

        void UpdateEnabled()
        {
            var isCustomPreset = m_NetworkConditioning.ConnectionType.Name == NetworkConditioningPreset.CustomPresetName;

            PacketDelaySlider.SetEnabled(isCustomPreset);
            PacketDelayField.SetEnabled(isCustomPreset);

            PacketJitterSlider.SetEnabled(isCustomPreset);
            PacketJitterField.SetEnabled(isCustomPreset);

            PacketLossSlider.SetEnabled(isCustomPreset);
            PacketLossField.SetEnabled(isCustomPreset);
        }

        void OnPresetSelected(ChangeEvent<string> changeEvent)
        {
            serializedObject.Update();

            var preset = NetworkConditioningPreset.ConnectionTypePresets.First(x => x.Name == changeEvent.newValue);
            UpdateConnectionTypeString(preset.Name);

            if (preset.Name != NetworkConditioningPreset.CustomPresetName)
            {
                UpdatePacketDelay(preset.PacketDelayMs);
                UpdatePacketJitter(preset.PacketJitterMs);
                UpdatePacketLoss(preset.PacketLossPercent);
            }

            serializedObject.ApplyModifiedProperties();

            UpdateEnabled();
            UpdateLiveIfPlaying();
        }

        void UpdateConnectionTypeString(string value)
        {
            var property = serializedObject.FindProperty(nameof(NetworkConditioning.ConnectionTypeString));
            property.stringValue = value;
        }

        void UpdatePacketDelay(int value)
        {
            PacketDelaySlider.SetValueWithoutNotify(value);
            PacketDelayField.SetValueWithoutNotify(value);

            var property = serializedObject.FindProperty(nameof(NetworkConditioning.PacketDelayMs));
            property.intValue = value;
        }

        void UpdatePacketJitter(int value)
        {
            PacketJitterSlider.SetValueWithoutNotify(value);
            PacketJitterField.SetValueWithoutNotify(value);

            var property = serializedObject.FindProperty(nameof(NetworkConditioning.PacketJitterMs));
            property.intValue = value;
        }

        void UpdatePacketLoss(int value)
        {
            PacketLossSlider.SetValueWithoutNotify(value);
            PacketLossField.SetValueWithoutNotify(value);

            var property = serializedObject.FindProperty(nameof(NetworkConditioning.PacketLossPercent));
            property.intValue = value;
        }

        void UpdateLiveIfPlaying()
        {
            if (Application.isPlaying)
            {
                m_NetworkConditioning.UpdateLiveParameters();
            }
        }
    }

    [Serializable]
    public class NetworkConditioningPreset
    {
        public const string CustomPresetName = "Custom";

        const string k_BroadbandDescription = "Typical of desktop and console platforms (and generally speaking most mobile players too).";
        const string k_PoorMobileDescription = "Extremely poor connection, completely unsuitable for synchronous multiplayer gaming due to exceptionally high ping. Turn based games may work.";
        const string k_MediumMobileDescription = "This is the minimum supported mobile connection for synchronous gameplay. Expect high pings, jitter, stuttering and packet loss.";
        const string k_DecentMobileDescription = "Suitable for synchronous multiplayer, except that ping (and overall connection quality and stability) may be quite poor.\n\nExpect to handle players dropping all packets in bursts of 1-60s. I.e. Ensure you handle reconnections.";
        const string k_GoodMobileDescription = "In many places, expect this to be 'as good as' or 'better than' home broadband.";

        public static readonly NetworkConditioningPreset[] ConnectionTypePresets =
        {
            new NetworkConditioningPreset("Home Broadband [WIFI, Cable, Console, PC]", k_BroadbandDescription, 2, 2, 1),
            new NetworkConditioningPreset("Mobile 2G [CDMA & GSM, '00]", k_PoorMobileDescription, 400, 200, 5),
            new NetworkConditioningPreset("Mobile 2.5G [GPRS, G, '00]", k_PoorMobileDescription, 200, 100, 5),
            new NetworkConditioningPreset("Mobile 2.75G [Edge, E, '06]", k_PoorMobileDescription, 200, 100, 5),
            new NetworkConditioningPreset("Mobile 3G [WCDMA & UMTS, '03]", k_PoorMobileDescription, 200, 100, 5),
            new NetworkConditioningPreset("Mobile 3.5G [HSDPA, H, '06]", k_MediumMobileDescription, 75, 50, 5),
            new NetworkConditioningPreset("Mobile 3.75G [HDSDPA+, H+, '11]", k_DecentMobileDescription, 75, 50, 5),
            new NetworkConditioningPreset("Mobile 4G [4G, LTE, '13]", k_DecentMobileDescription, 50, 25, 3),
            new NetworkConditioningPreset("Mobile 4.5G [4G+, LTE-A, '16]", k_DecentMobileDescription, 50, 25, 3),
            new NetworkConditioningPreset("Mobile 5G ['20]", k_GoodMobileDescription, 1, 10, 1),
            new NetworkConditioningPreset(CustomPresetName, string.Empty, 0, 0, 0),
        };

        public NetworkConditioningPreset(
            string name,
            string descriptions,
            int packetDelayMs,
            int packetJitterMs,
            int packetLossPercent)
        {
            Name = name;
            Description = descriptions;
            PacketDelayMs = packetDelayMs;
            PacketJitterMs = packetJitterMs;
            PacketLossPercent = packetLossPercent;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public int PacketDelayMs { get; set; }

        public int PacketJitterMs { get; set; }

        public int PacketLossPercent { get; set; }
    }
}

#endif

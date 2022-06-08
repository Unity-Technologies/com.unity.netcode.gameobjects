using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Netcode.Transports.UTP
{
    public class NetworkTypeView : VisualElement
    {
        const string UXML = "Packages/com.unity.netcode.gameobjects/Runtime/Transports/UTP/NetworkTypeView.uxml";
        const string Custom = nameof(Custom);

        readonly NetworkSimulator m_NetworkSimulator;

        DropdownField PresetDropdown => this.Q<DropdownField>(nameof(PresetDropdown));
        ObjectField CustomPresetValue => this.Q<ObjectField>(nameof(CustomPresetValue));
        SliderInt PacketDelaySlider => this.Q<SliderInt>(nameof(PacketDelaySlider));
        SliderInt PacketJitterSlider => this.Q<SliderInt>(nameof(PacketJitterSlider));
        SliderInt PacketLossSlider => this.Q<SliderInt>(nameof(PacketLossSlider));

        bool m_CustomSelected;

        public NetworkTypeView(NetworkSimulator networkSimulator)
        {
            m_NetworkSimulator = networkSimulator;
            if (m_NetworkSimulator.NetworkTypeConfiguration == null)
            {
                m_NetworkSimulator.NetworkTypeConfiguration = NetworkTypePresets.None;
            }

            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);

            var presets = NetworkTypePresets.Values.Select(x => x.Name).ToList();
            presets.Add(Custom);

            PresetDropdown.choices = presets;
            PresetDropdown.index = HasCustomValue
                    ? PresetDropdown.choices.IndexOf(Custom)
                    : PresetDropdown.choices.IndexOf(m_NetworkSimulator.NetworkTypeConfiguration.Name);
            PresetDropdown.RegisterCallback<ChangeEvent<string>>(OnPresetSelected);

            CustomPresetValue.objectType = typeof(NetworkTypeConfiguration);
            if (HasCustomValue)
            {
                CustomPresetValue.value = m_NetworkSimulator.NetworkTypeConfiguration;
            }

            CustomPresetValue.RegisterCallback<ChangeEvent<Object>>(evt =>
            {
                m_NetworkSimulator.NetworkTypeConfiguration = evt.newValue as NetworkTypeConfiguration;
                UpdateEnabled();
                UpdatePacketDelay(m_NetworkSimulator.NetworkTypeConfiguration.PacketDelayMs);
                UpdatePacketJitter(m_NetworkSimulator.NetworkTypeConfiguration.PacketJitterMs);
                UpdatePacketLoss(m_NetworkSimulator.NetworkTypeConfiguration.PacketLossPercent);
            });

            PacketDelaySlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketDelay(change.newValue));
            PacketJitterSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketJitter(change.newValue));
            PacketLossSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketLoss(change.newValue));

            UpdatePacketDelay(m_NetworkSimulator.NetworkTypeConfiguration.PacketDelayMs);
            UpdatePacketJitter(m_NetworkSimulator.NetworkTypeConfiguration.PacketJitterMs);
            UpdatePacketLoss(m_NetworkSimulator.NetworkTypeConfiguration.PacketLossPercent);

            UpdateEnabled();
        }

        bool HasValue => m_NetworkSimulator.NetworkTypeConfiguration != null;

        bool HasCustomValue => HasValue
                               && NetworkTypePresets.Values.All(
                                x => x.Name != m_NetworkSimulator.NetworkTypeConfiguration.Name);

        void UpdateEnabled()
        {
            CustomPresetValue.style.display = HasCustomValue || m_CustomSelected
                ? new StyleEnum<DisplayStyle>(StyleKeyword.Auto)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);

            PacketDelaySlider.SetEnabled(HasCustomValue);
            PacketJitterSlider.SetEnabled(HasCustomValue);
            PacketLossSlider.SetEnabled(HasCustomValue);
        }

        void OnPresetSelected(ChangeEvent<string> changeEvent)
        {
            if (changeEvent.newValue == Custom)
            {
                m_CustomSelected = true;
            }
            else
            {
                m_CustomSelected = false;

                var preset = NetworkTypePresets.Values.First(x => x.Name == changeEvent.newValue);

                UpdatePacketDelay(preset.PacketDelayMs);
                UpdatePacketJitter(preset.PacketJitterMs);
                UpdatePacketLoss(preset.PacketLossPercent);
            }

            UpdateEnabled();
            UpdateLiveIfPlaying();
        }

        void UpdatePacketDelay(int value)
        {
            PacketDelaySlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketDelayMs = value;

            UpdateLiveIfPlaying();
        }

        void UpdatePacketJitter(int value)
        {
            PacketJitterSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketJitterMs = value;

            UpdateLiveIfPlaying();
        }

        void UpdatePacketLoss(int value)
        {
            PacketLossSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketLossPercent = value;

            UpdateLiveIfPlaying();
        }

        void UpdateLiveIfPlaying()
        {
            if (Application.isPlaying)
            {
                m_NetworkSimulator.UpdateLiveParameters();
            }
        }
    }
}
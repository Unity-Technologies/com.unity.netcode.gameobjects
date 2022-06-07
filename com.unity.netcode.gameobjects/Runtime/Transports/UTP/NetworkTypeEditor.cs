using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Netcode.Transports.UTP
{
    public class NetworkTypeEditor : VisualElement
    {
        const string UXML = "Packages/com.unity.netcode.gameobjects/Runtime/Transports/UTP/NetworkTypeEditor.uxml";
        const string Custom = nameof(Custom);

        NetworkSimulator m_NetworkSimulator;

        DropdownField PresetDropdown => this.Q<DropdownField>(nameof(PresetDropdown));
        ObjectField CustomPresetValue => this.Q<ObjectField>(nameof(CustomPresetValue));
        SliderInt PacketDelaySlider => this.Q<SliderInt>(nameof(PacketDelaySlider));
        IntegerField PacketDelayField => this.Q<IntegerField>(nameof(PacketDelayField));
        SliderInt PacketJitterSlider => this.Q<SliderInt>(nameof(PacketJitterSlider));
        IntegerField PacketJitterField => this.Q<IntegerField>(nameof(PacketJitterField));
        SliderInt PacketLossSlider => this.Q<SliderInt>(nameof(PacketLossSlider));
        IntegerField PacketLossField => this.Q<IntegerField>(nameof(PacketLossField));

        bool m_CustomSelected;

        public NetworkTypeEditor(NetworkSimulator networkSimulator)
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
            PacketDelayField.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketDelay(change.newValue));
            PacketJitterSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketJitter(change.newValue));
            PacketJitterField.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketJitter(change.newValue));
            PacketLossSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketLoss(change.newValue));
            PacketLossField.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketLoss(change.newValue));

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
            PacketDelayField.SetEnabled(HasCustomValue);

            PacketJitterSlider.SetEnabled(HasCustomValue);
            PacketJitterField.SetEnabled(HasCustomValue);

            PacketLossSlider.SetEnabled(HasCustomValue);
            PacketLossField.SetEnabled(HasCustomValue);
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
            PacketDelayField.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketDelayMs = value;
        }

        void UpdatePacketJitter(int value)
        {
            PacketJitterSlider.SetValueWithoutNotify(value);
            PacketJitterField.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketJitterMs = value;
        }

        void UpdatePacketLoss(int value)
        {
            PacketLossSlider.SetValueWithoutNotify(value);
            PacketLossField.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketLossPercent = value;
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
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Netcode.Simulator
{
    public class NetworkTypeView : VisualElement
    {
        const string UXML = "Packages/com.unity.netcode.gameobjects/Runtime/Simulator/NetworkTypeView.uxml";
        const string Custom = nameof(Custom);

        readonly NetworkSimulator m_NetworkSimulator;

        DropdownField PresetDropdown => this.Q<DropdownField>(nameof(PresetDropdown));
        ObjectField CustomPresetValue => this.Q<ObjectField>(nameof(CustomPresetValue));
        SliderInt PacketDelaySlider => this.Q<SliderInt>(nameof(PacketDelaySlider));
        SliderInt PacketJitterSlider => this.Q<SliderInt>(nameof(PacketJitterSlider));
        SliderInt PacketLossIntervalSlider => this.Q<SliderInt>(nameof(PacketLossIntervalSlider));
        SliderInt PacketLossPercentSlider => this.Q<SliderInt>(nameof(PacketLossPercentSlider));
        SliderInt PacketDuplicationPercentSlider => this.Q<SliderInt>(nameof(PacketDuplicationPercentSlider));
        SliderInt PacketFuzzFactorSlider => this.Q<SliderInt>(nameof(PacketFuzzFactorSlider));
        SliderInt PacketFuzzOffsetSlider => this.Q<SliderInt>(nameof(PacketFuzzOffsetSlider));

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
            CustomPresetValue.RegisterCallback<ChangeEvent<Object>>(OnCustomPresetChanged);

            PacketDelaySlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketDelay(change.newValue));
            PacketJitterSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketJitter(change.newValue));
            PacketLossIntervalSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketLossInterval(change.newValue));
            PacketLossPercentSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketLossPercent(change.newValue));
            PacketDuplicationPercentSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketDuplicationPercent(change.newValue));
            PacketFuzzFactorSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketFuzzFactor(change.newValue));
            PacketFuzzOffsetSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketFuzzOffset(change.newValue));

            UpdateSliders(m_NetworkSimulator.NetworkTypeConfiguration);

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
            PacketLossIntervalSlider.SetEnabled(HasCustomValue);
            PacketLossPercentSlider.SetEnabled(HasCustomValue);
            PacketDuplicationPercentSlider.SetEnabled(HasCustomValue);
            PacketFuzzFactorSlider.SetEnabled(HasCustomValue);
            PacketFuzzOffsetSlider.SetEnabled(HasCustomValue);
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

                UpdateSliders(preset);
            }

            UpdateEnabled();
            UpdateLiveIfPlaying();
        }

        void OnCustomPresetChanged(ChangeEvent<Object> evt)
        {
            m_NetworkSimulator.NetworkTypeConfiguration = evt.newValue as NetworkTypeConfiguration;

            UpdateEnabled();

            UpdateSliders(m_NetworkSimulator.NetworkTypeConfiguration);
        }

        void UpdateSliders(NetworkTypeConfiguration configuration)
        {
            UpdatePacketDelay(configuration.PacketDelayMs);
            UpdatePacketJitter(configuration.PacketJitterMs);
            UpdatePacketLossInterval(configuration.PacketLossInterval);
            UpdatePacketLossPercent(configuration.PacketLossPercent);
            UpdatePacketDuplicationPercent(configuration.PacketDuplicationPercent);
            UpdatePacketFuzzFactor(configuration.PacketFuzzFactor);
            UpdatePacketFuzzOffset(configuration.PacketFuzzOffset);
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

        void UpdatePacketLossInterval(int value)
        {
            PacketLossIntervalSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketLossInterval = value;

            UpdateLiveIfPlaying();
        }

        void UpdatePacketLossPercent(int value)
        {
            PacketLossPercentSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketLossPercent = value;

            UpdateLiveIfPlaying();
        }

        void UpdatePacketDuplicationPercent(int value)
        {
            PacketDuplicationPercentSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketDuplicationPercent = value;

            UpdateLiveIfPlaying();
        }

        void UpdatePacketFuzzFactor(int value)
        {
            PacketFuzzFactorSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketFuzzFactor = value;

            UpdateLiveIfPlaying();
        }

        void UpdatePacketFuzzOffset(int value)
        {
            PacketFuzzOffsetSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.NetworkTypeConfiguration.PacketFuzzOffset = value;

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
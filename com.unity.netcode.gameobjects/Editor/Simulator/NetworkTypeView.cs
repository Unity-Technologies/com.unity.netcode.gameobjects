using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Netcode.Editor
{
    public class NetworkTypeView : VisualElement
    {
        const string UXML = "Packages/com.unity.netcode.gameobjects/Editor/Simulator/NetworkTypeView.uxml";
        const string Custom = nameof(Custom);

        readonly NetworkSimulator m_NetworkSimulator;

        DropdownField PresetDropdown => this.Q<DropdownField>(nameof(PresetDropdown));
        ObjectField CustomPresetValue => this.Q<ObjectField>(nameof(CustomPresetValue));
        SliderInt PacketDelaySlider => this.Q<SliderInt>(nameof(PacketDelaySlider));
        SliderInt PacketJitterSlider => this.Q<SliderInt>(nameof(PacketJitterSlider));
        SliderInt PacketLossIntervalSlider => this.Q<SliderInt>(nameof(PacketLossIntervalSlider));
        SliderInt PacketLossPercentSlider => this.Q<SliderInt>(nameof(PacketLossPercentSlider));
        SliderInt PacketDuplicationPercentSlider => this.Q<SliderInt>(nameof(PacketDuplicationPercentSlider));

        bool m_CustomSelected;

        public NetworkTypeView(NetworkSimulator networkSimulator)
        {
            m_NetworkSimulator = networkSimulator;
            if (m_NetworkSimulator.SimulatorConfiguration == null)
            {
                SetSimulatorConfiguration(NetworkTypePresets.None);
            }

            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);

            var presets = NetworkTypePresets.Values.Select(x => x.Name).ToList();
            presets.Add(Custom);

            PresetDropdown.choices = presets;
            PresetDropdown.index = HasCustomValue
                ? PresetDropdown.choices.IndexOf(Custom)
                : PresetDropdown.choices.IndexOf(m_NetworkSimulator.SimulatorConfiguration.Name);
            PresetDropdown.RegisterCallback<ChangeEvent<string>>(OnPresetSelected);

            CustomPresetValue.objectType = typeof(NetworkSimulatorConfiguration);
            if (HasCustomValue)
            {
                CustomPresetValue.value = m_NetworkSimulator.SimulatorConfiguration;
            }
            CustomPresetValue.RegisterCallback<ChangeEvent<Object>>(OnCustomPresetChanged);

            PacketDelaySlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketDelay(change.newValue));
            PacketJitterSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketJitter(change.newValue));
            PacketLossIntervalSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketLossInterval(change.newValue));
            PacketLossPercentSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketLossPercent(change.newValue));
            PacketDuplicationPercentSlider.RegisterCallback<ChangeEvent<int>>(change => UpdatePacketDuplicationPercent(change.newValue));

            UpdateSliders(m_NetworkSimulator.SimulatorConfiguration);

            UpdateEnabled();
        }

        bool HasValue => m_NetworkSimulator.SimulatorConfiguration != null;

        bool HasCustomValue => HasValue && NetworkTypePresets.Values.Any(SimulatorConfigurationMatchesPresetName);
        
        bool SimulatorConfigurationMatchesPresetName(NetworkSimulatorConfiguration configuration)
        {
            return configuration.Name == m_NetworkSimulator.SimulatorConfiguration.Name;
        }

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
                SetSimulatorConfiguration(preset);
                UpdateSliders(preset);
            }

            UpdateEnabled();
            UpdateLiveIfPlaying();
        }

        void OnCustomPresetChanged(ChangeEvent<Object> evt)
        {
            var configuration = evt.newValue as NetworkSimulatorConfiguration;
            SetSimulatorConfiguration(configuration);

            UpdateEnabled();

            UpdateSliders(m_NetworkSimulator.SimulatorConfiguration);
        }

        void UpdateSliders(NetworkSimulatorConfiguration configuration)
        {
            UpdatePacketDelay(configuration.PacketDelayMs);
            UpdatePacketJitter(configuration.PacketJitterMs);
            UpdatePacketLossInterval(configuration.PacketLossInterval);
            UpdatePacketLossPercent(configuration.PacketLossPercent);
            UpdatePacketDuplicationPercent(configuration.PacketDuplicationPercent);
        }

        void UpdatePacketDelay(int value)
        {
            PacketDelaySlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.SimulatorConfiguration.PacketDelayMs = value;

            UpdateLiveIfPlaying();
        }

        void UpdatePacketJitter(int value)
        {
            PacketJitterSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.SimulatorConfiguration.PacketJitterMs = value;

            UpdateLiveIfPlaying();
        }

        void UpdatePacketLossInterval(int value)
        {
            PacketLossIntervalSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.SimulatorConfiguration.PacketLossInterval = value;

            UpdateLiveIfPlaying();
        }

        void UpdatePacketLossPercent(int value)
        {
            PacketLossPercentSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.SimulatorConfiguration.PacketLossPercent = value;

            UpdateLiveIfPlaying();
        }

        void UpdatePacketDuplicationPercent(int value)
        {
            PacketDuplicationPercentSlider.SetValueWithoutNotify(value);

            m_NetworkSimulator.SimulatorConfiguration.PacketDuplicationPercent = value;

            UpdateLiveIfPlaying();
        }

        void UpdateLiveIfPlaying()
        {
            if (Application.isPlaying)
            {
                m_NetworkSimulator.UpdateLiveParameters();
            }
        }
        
        void SetSimulatorConfiguration(NetworkSimulatorConfiguration configuration)
        {
            m_NetworkSimulator.SimulatorConfiguration = configuration;
            EditorUtility.SetDirty(m_NetworkSimulator);
        }
    }
}

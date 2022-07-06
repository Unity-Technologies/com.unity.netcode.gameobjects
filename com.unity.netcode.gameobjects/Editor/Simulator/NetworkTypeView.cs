using System.ComponentModel;
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

        readonly SerializedProperty m_SerializedProperty;
        bool m_CustomSelected;

        bool HasValue => m_NetworkSimulator.SimulatorConfiguration != null;

        bool HasCustomValue => HasValue && NetworkTypePresets.Values.Any(SimulatorConfigurationMatchesPresetName) == false;

        public NetworkTypeView(SerializedProperty serializedProperty, NetworkSimulator networkSimulator)
        {
            m_NetworkSimulator = networkSimulator;
            m_SerializedProperty = serializedProperty;
            m_SerializedProperty.serializedObject.Update();


            if (m_NetworkSimulator.SimulatorConfiguration == null)
            {
                SetSimulatorConfiguration(NetworkTypePresets.None);
            }

            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);

            UpdatePresetDropdown();
            CustomPresetValue.objectType = typeof(NetworkSimulatorConfiguration);

            if (HasCustomValue)
            {
                CustomPresetValue.value = m_NetworkSimulator.SimulatorConfiguration;
            }

            UpdateSliders(m_NetworkSimulator.SimulatorConfiguration);
            UpdateEnabled();
            
            this.AddEventLifecycle(OnAttach, OnDetach);
        }
        void OnAttach(AttachToPanelEvent evt)
        {
            CustomPresetValue.RegisterCallback<ChangeEvent<Object>>(OnCustomPresetChange);
            PacketDelaySlider.RegisterCallback<ChangeEvent<int>>(OnPackageDelayChange);
            PacketJitterSlider.RegisterCallback<ChangeEvent<int>>(OnPacketJitterChanged);
            PacketLossIntervalSlider.RegisterCallback<ChangeEvent<int>>(OnPacketLossIntervalChange);
            PacketLossPercentSlider.RegisterCallback<ChangeEvent<int>>(OnPacketLossPercentChange);
            PacketDuplicationPercentSlider.RegisterCallback<ChangeEvent<int>>(OnPacketDuplicationChange);
            PresetDropdown.RegisterCallback<ChangeEvent<string>>(OnPresetSelected);
            
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            m_NetworkSimulator.PropertyChanged += OnNetworkSimulatorPropertyChanged;
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            CustomPresetValue.UnregisterCallback<ChangeEvent<Object>>(OnCustomPresetChange);
            PacketDelaySlider.UnregisterCallback<ChangeEvent<int>>(OnPackageDelayChange);
            PacketJitterSlider.UnregisterCallback<ChangeEvent<int>>(OnPacketJitterChanged);
            PacketLossIntervalSlider.UnregisterCallback<ChangeEvent<int>>(OnPacketLossIntervalChange);
            PacketLossPercentSlider.UnregisterCallback<ChangeEvent<int>>(OnPacketLossPercentChange);
            PacketDuplicationPercentSlider.UnregisterCallback<ChangeEvent<int>>(OnPacketDuplicationChange);
            PresetDropdown.RegisterCallback<ChangeEvent<string>>(OnPresetSelected);
            
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            m_NetworkSimulator.PropertyChanged -= OnNetworkSimulatorPropertyChanged;
        }

        void OnPackageDelayChange(ChangeEvent<int> change)
        {
            UpdatePacketDelay(change.newValue);
        }

        void OnPacketJitterChanged(ChangeEvent<int> change)
        {
            UpdatePacketJitter(change.newValue);
        }

        void OnPacketLossIntervalChange(ChangeEvent<int> change)
        {
            UpdatePacketLossInterval(change.newValue);
        }

        void OnPacketLossPercentChange(ChangeEvent<int> change)
        {
            UpdatePacketLossPercent(change.newValue);
        }

        void OnPacketDuplicationChange(ChangeEvent<int> change)
        {
            UpdatePacketDuplicationPercent(change.newValue);
        }

        void OnUndoRedoPerformed()
        {
            UpdatePresetDropdown();
        }

        void OnNetworkSimulatorPropertyChanged(object sender, PropertyChangedEventArgs _)
        {
            UpdatePresetDropdown();
        }

        void UpdatePresetDropdown()
        {
            var presets = NetworkTypePresets.Values.Select(x => x.Name).ToList();
            presets.Add(Custom);

            PresetDropdown.choices = presets;
            PresetDropdown.index = HasCustomValue
                ? PresetDropdown.choices.IndexOf(Custom)
                : PresetDropdown.choices.IndexOf(m_NetworkSimulator.SimulatorConfiguration.Name);
        }

        bool SimulatorConfigurationMatchesPresetName(NetworkSimulatorConfiguration configuration)
        {
            return configuration.Name == m_NetworkSimulator.SimulatorConfiguration.Name;
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

        void OnCustomPresetChange(ChangeEvent<Object> evt)
        {
            var configuration = evt.newValue as NetworkSimulatorConfiguration;
            SetSimulatorConfiguration(configuration);

            UpdateEnabled();

            UpdateSliders(m_NetworkSimulator.SimulatorConfiguration);
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
            m_SerializedProperty.objectReferenceValue = configuration;
            m_SerializedProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}

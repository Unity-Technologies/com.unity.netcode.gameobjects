using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Netcode.Simulator
{
    public class NetworkScenarioView : VisualElement
    {
        const string UXML = "Packages/com.unity.netcode.gameobjects/Runtime/Simulator/NetworkScenarioView.uxml";
        const string None = nameof(None);

        readonly NetworkSimulator m_NetworkSimulator;
        readonly List<Type> m_Scenarios;

        DropdownField PresetDropdown => this.Q<DropdownField>(nameof(PresetDropdown));

        public NetworkScenarioView(NetworkSimulator networkSimulator)
        {
            m_NetworkSimulator = networkSimulator;

            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);

            m_Scenarios = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.GetInterfaces().Contains(typeof(INetworkSimulatorScenario)))
                .ToList();

            var choices = new List<string> { None };
            choices.AddRange(m_Scenarios.Select(x => x.Name));

            PresetDropdown.choices = choices;
            PresetDropdown.index = m_NetworkSimulator.NetworkSimulatorScenario == null
                ? 0
                : choices.IndexOf(m_NetworkSimulator.NetworkSimulatorScenario.GetType().Name);
            PresetDropdown.RegisterCallback<ChangeEvent<string>>(OnPresetSelected);
        }

        void OnPresetSelected(ChangeEvent<string> changeEvent)
        {
            if (changeEvent.newValue == None)
            {
                m_NetworkSimulator.NetworkSimulatorScenario = null;
            }
            else
            {
                var scenario = m_Scenarios.First(x => x.Name == changeEvent.newValue);
                var instance = Activator.CreateInstance(scenario);
                m_NetworkSimulator.NetworkSimulatorScenario = instance as INetworkSimulatorScenario;
            }
        }
    }
}
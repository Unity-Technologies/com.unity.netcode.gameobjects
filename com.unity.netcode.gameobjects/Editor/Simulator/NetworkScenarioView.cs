using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Netcode.Editor
{
    public class NetworkScenarioView : VisualElement
    {
        const string UXML = "Packages/com.unity.netcode.gameobjects/Editor/Simulator/NetworkScenarioView.uxml";
        const string None = nameof(None);

        readonly NetworkScenario m_NetworkScenario;
        readonly List<Type> m_Scenarios;
        readonly SerializedProperty m_NetworkScenarioProperty;
        readonly List<string> m_Choices;

        DropdownField ScenarioDropdown => this.Q<DropdownField>(nameof(ScenarioDropdown));

        public NetworkScenarioView(SerializedProperty networkScenarioProperty, NetworkScenario networkScenario)
        {
            m_NetworkScenario = networkScenario;
            m_NetworkScenarioProperty = networkScenarioProperty;

            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);
            
            // Using IMGUIContainer since the PropertyField doesn't properly support SerializedReferences until Unity 2022.1
            // Source: https://forum.unity.com/threads/propertyfields-dont-work-properly-with-serializereference-fields.796725/
            var scenarioParameters = new IMGUIContainer(OnScenarioParametersGUIHandler);
            Add(scenarioParameters);

            m_Scenarios = FindScenarios();
            m_Choices = new() { None };
            m_Choices.AddRange(m_Scenarios.Select(x => x.Name));

            ScenarioDropdown.choices = m_Choices;
            UpdateScenarioDropdown();
            
            this.AddEventLifecycle(OnAttach, OnDetach);
        }
        
        void OnAttach(AttachToPanelEvent evt)
        {
            ScenarioDropdown.RegisterCallback<ChangeEvent<string>>(OnPresetSelected);
            
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            ScenarioDropdown.UnregisterCallback<ChangeEvent<string>>(OnPresetSelected);
            
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        void UndoRedoPerformed()
        {
            UpdateScenarioDropdown();
        }

        void OnScenarioParametersGUIHandler()
        {
            m_NetworkScenarioProperty.serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_NetworkScenarioProperty, true);
            
            if (EditorGUI.EndChangeCheck())
            {
                m_NetworkScenarioProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        void OnPresetSelected(ChangeEvent<string> changeEvent)
        {
            if (changeEvent.newValue == None)
            {
                m_NetworkScenario.NetworkSimulatorScenario = null;
                return;
            }

            var scenario = m_Scenarios.First(x => x.Name == changeEvent.newValue);
            var instance = Activator.CreateInstance(scenario);
            m_NetworkScenarioProperty.managedReferenceValue = (INetworkSimulatorScenario)instance;
            m_NetworkScenarioProperty.serializedObject.ApplyModifiedProperties();
        }

        void UpdateScenarioDropdown()
        {
            ScenarioDropdown.index = m_NetworkScenario.NetworkSimulatorScenario == null
                ? 0
                : m_Choices.IndexOf(m_NetworkScenario.NetworkSimulatorScenario.GetType().Name);
        }

        bool TypeIsValidNetworkScenario(Type type)
        {
            return type.IsClass && type.IsAbstract == false && typeof(INetworkSimulatorScenario).IsAssignableFrom(type);
        }

        List<Type> FindScenarios() => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(TypeIsValidNetworkScenario)
            .ToList();
    }
}
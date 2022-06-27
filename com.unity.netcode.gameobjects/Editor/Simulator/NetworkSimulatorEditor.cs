using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Netcode.Editor
{
    [CustomEditor(typeof(NetworkSimulator))]
    public class NetworkSimulatorEditor : UnityEditor.Editor
    {
        NetworkSimulator m_NetworkSimulator;

        VisualElement m_Inspector;

        public override VisualElement CreateInspectorGUI()
        {
            m_NetworkSimulator = target as NetworkSimulator;

            m_Inspector = new VisualElement();

            m_Inspector.Add(new NetworkEventsView(
                new NetworkEventsApi(m_NetworkSimulator, m_NetworkSimulator.GetComponent<UnityTransport>())));

            var simulatorConfigurationProperty = serializedObject.FindProperty(nameof(NetworkSimulator.m_SimulatorConfiguration));
            m_Inspector.Add(new NetworkTypeView(simulatorConfigurationProperty, m_NetworkSimulator));

            return m_Inspector;
        }
    }
}

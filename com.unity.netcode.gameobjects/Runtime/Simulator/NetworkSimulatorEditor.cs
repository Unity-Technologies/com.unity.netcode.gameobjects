using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Netcode.Simulator
{
    [CustomEditor(typeof(NetworkSimulator))]
    public class NetworkSimulatorEditor : Editor
    {
        NetworkSimulator m_NetworkSimulator;

        VisualElement m_Inspector;

        public override VisualElement CreateInspectorGUI()
        {
            m_NetworkSimulator = target as NetworkSimulator;

            m_Inspector = new VisualElement();
            m_Inspector.Add(new NetworkEventsView(
                new NetworkEventsApi(m_NetworkSimulator, m_NetworkSimulator.GetComponent<UnityTransport>())));
            m_Inspector.Add(new NetworkTypeView(m_NetworkSimulator));
            m_Inspector.Add(new NetworkScenarioView(m_NetworkSimulator));

            return m_Inspector;
        }
    }
}
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Netcode.Transports.UTP
{
    [CustomEditor(typeof(NetworkSimulator))]
    public class NetworkSimulatorEditor : Editor
    {
        NetworkSimulator m_NetworkConditioning;

        VisualElement m_Inspector;

        public override VisualElement CreateInspectorGUI()
        {
            m_NetworkConditioning = target as NetworkSimulator;

            m_Inspector = new VisualElement();
            m_Inspector.Add(new NetworkEventsView(new NoOpNetworkEventsApi()));
            m_Inspector.Add(new NetworkTypeView(m_NetworkConditioning));

            return m_Inspector;
        }
    }
}
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Netcode.Editor
{
    [CustomEditor(typeof(NetworkScenario))]
    public class NetworkScenarioEditor : UnityEditor.Editor
    {
        private NetworkScenario m_NetworkScenario;

        private VisualElement m_Inspector;

        public override VisualElement CreateInspectorGUI()
        {
            m_NetworkScenario = target as NetworkScenario;

            m_Inspector = new VisualElement();
            m_Inspector.Add(new NetworkEventsView(new NoOpNetworkEventsApi()));
            m_Inspector.Add(new NetworkScenarioView(m_NetworkScenario));

            return m_Inspector;
        }
    }
}

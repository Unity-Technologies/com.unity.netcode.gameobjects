using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Netcode.Editor
{
    [CustomEditor(typeof(NetworkScenario))]
    public class NetworkScenarioEditor : UnityEditor.Editor
    {
        NetworkScenario m_NetworkScenario;

        VisualElement m_Inspector;

        public override VisualElement CreateInspectorGUI()
        {
            m_NetworkScenario = (NetworkScenario)target;

            m_Inspector = new VisualElement();
            
            var networkScenarioProperty = serializedObject.FindProperty(nameof(m_NetworkScenario.m_NetworkSimulatorScenario));
            m_Inspector.Add(new NetworkScenarioView(networkScenarioProperty, m_NetworkScenario));

            return m_Inspector;
        }
    }
}

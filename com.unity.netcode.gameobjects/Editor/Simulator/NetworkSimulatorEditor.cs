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
            m_NetworkSimulator = (NetworkSimulator)target;
            
            m_Inspector = new VisualElement();
            m_Inspector.Add(new NetworkEventsView(m_NetworkSimulator.NetworkEventsApi));
            m_Inspector.Add(new NetworkTypeView(serializedObject, m_NetworkSimulator));
            
            return m_Inspector;
        }
    }
}

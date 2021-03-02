using MLAPI;
using MLAPI.LagCompensation;

namespace UnityEditor
{
    [CustomEditor(typeof(TrackedObject), true)]
    [CanEditMultipleObjects]
    public class TrackedObjectEditor : Editor
    {
        private TrackedObject m_TrackedObject;
        private bool m_Initialized;

        private void Init()
        {
            if (m_Initialized) return;

            m_TrackedObject = (TrackedObject)target;
            m_Initialized = true;
        }

        public override void OnInspectorGUI()
        {
            Init();

            base.OnInspectorGUI();
            if (!ReferenceEquals(NetworkManager.Singleton, null) && NetworkManager.Singleton.IsServer)
            {
                EditorGUILayout.LabelField("Total points: ", m_TrackedObject.TotalPoints.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("Avg time between points: ", m_TrackedObject.AvgTimeBetweenPointsMs + " ms", EditorStyles.label);
                EditorGUILayout.LabelField("Total history: ", m_TrackedObject.TotalTimeHistory + " seconds", EditorStyles.label);
            }

            Repaint();
        }
    }
}
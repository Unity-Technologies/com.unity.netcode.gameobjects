using MLAPI.MonoBehaviours.Core;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(TrackedObject), true)]
    [CanEditMultipleObjects]
    public class TrackedObjectEditor : Editor
    {
        private TrackedObject trackedObject;
        private bool initialized;

        private void Init()
        {
            if (initialized)
                return;

            trackedObject = (TrackedObject)target;
            initialized = true;
        }

        public override void OnInspectorGUI()
        {
            Init();
            base.OnInspectorGUI();
            if(NetworkingManager.singleton != null && NetworkingManager.singleton.isServer)
            {
                EditorGUILayout.LabelField("Total points: ", trackedObject.TotalPoints.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("Avg time between points: ", trackedObject.AvgTimeBetweenPointsMs.ToString() + " ms", EditorStyles.label);
            }
            Repaint();
        }
    }
}

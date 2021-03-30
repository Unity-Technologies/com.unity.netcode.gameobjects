using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MLAPI.Transports
{
    /// <summary>
    /// A transport channel used by the MLAPI
    /// </summary>
    [Serializable]
    public class UNetChannel
    {
        /// <summary>
        /// The name of the channel
        /// </summary>
        [ReadOnly]
        public byte Id;

        /// <summary>
        /// The type of channel
        /// </summary>
        public QosType Type;

        private class ReadOnlyAttribute : PropertyAttribute { }

        [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
        private class ReadOnlyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                // Saving previous GUI enabled value
                var previousGUIState = GUI.enabled;

                // Disabling edit for property
                GUI.enabled = false;

                // Drawing Property
                EditorGUI.PropertyField(position, property, label);

                // Setting old GUI enabled value
                GUI.enabled = previousGUIState;
            }
        }
    }
}

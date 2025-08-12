
#if COM_UNITY_MODULES_ANIMATION
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    [CustomPropertyDrawer(typeof(NetworkAnimator.AnimatorParametersListContainer))]
    internal class NetworkAnimatorParameterEntryDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw the foldout for the list
            SerializedProperty items = property.FindPropertyRelative(nameof(NetworkAnimator.AnimatorParameterEntries.ParameterEntries));
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);

            if (property.isExpanded)
            {
                // Set the indention level down
                EditorGUI.indentLevel++;
                for (int i = 0; i < items.arraySize; i++)
                {
                    position.y += EditorGUIUtility.singleLineHeight + 2;
                    SerializedProperty element = items.GetArrayElementAtIndex(i);
                    var nameField = element.FindPropertyRelative(nameof(NetworkAnimator.AnimatorParameterEntry.name));
                    // Draw the foldout for the item
                    element.isExpanded = EditorGUI.Foldout(position, element.isExpanded, nameField.stringValue);
                    if (!element.isExpanded)
                    {
                        continue;
                    }
                    // Draw the contents of the item
                    position.y += EditorGUIUtility.singleLineHeight + 2;
                    // Set the indention level down
                    EditorGUI.indentLevel++;
                    // Calculate rects
                    var nameHashRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                    position.y += EditorGUIUtility.singleLineHeight + 2;
                    var paramRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                    position.y += EditorGUIUtility.singleLineHeight + 2;
                    var syncRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

                    // Get the three properties we want to visualize in the inspector view
                    var synchronizeField = element.FindPropertyRelative(nameof(NetworkAnimator.AnimatorParameterEntry.Synchronize));
                    var nameHashField = element.FindPropertyRelative(nameof(NetworkAnimator.AnimatorParameterEntry.NameHash));
                    var parameterTypeField = element.FindPropertyRelative(nameof(NetworkAnimator.AnimatorParameterEntry.ParameterType));

                    // Draw the read only fields
                    GUI.enabled = false;
                    EditorGUI.PropertyField(nameHashRect, nameHashField);
                    EditorGUI.PropertyField(paramRect, parameterTypeField);
                    GUI.enabled = true;
                    // Draw the read/write fields
                    EditorGUI.PropertyField(syncRect, synchronizeField);
                    // Set the indention level up
                    EditorGUI.indentLevel--;
                }
                // Set the indention level up
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var totalHeight = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return totalHeight;
            }
            var singleLineWithSpace = EditorGUIUtility.singleLineHeight + 2;
            SerializedProperty items = property.FindPropertyRelative(nameof(NetworkAnimator.AnimatorParameterEntries.ParameterEntries));

            totalHeight += singleLineWithSpace;
            for (int i = 0; i < items.arraySize; i++)
            {
                SerializedProperty element = items.GetArrayElementAtIndex(i);
                if (element.isExpanded)
                {
                    totalHeight += (singleLineWithSpace * 4);
                }
                else
                {
                    totalHeight += EditorGUIUtility.singleLineHeight;
                }
            }
            return totalHeight;
        }
    }
}
#endif

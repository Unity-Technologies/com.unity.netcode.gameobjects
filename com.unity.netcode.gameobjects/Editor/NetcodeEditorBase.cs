using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// The base Netcode Editor helper class to display derived <see cref="MonoBehaviour"/> based components <br />
    /// where each child generation's properties will be displayed within a FoldoutHeaderGroup.
    /// </summary>
    [CanEditMultipleObjects]
    public partial class NetcodeEditorBase<TT> : UnityEditor.Editor where TT : MonoBehaviour
    {
        private const int k_IndentOffset = 15;

        /// <inheritdoc cref="UnityEditor.Editor.OnEnable"/>
        public virtual void OnEnable()
        {
        }

        /// <summary>
        /// Will draw a property field with an indention level using the default or a specified offset per indention.
        /// </summary>
        /// <param name="property">The serialized property to draw as a default field</param>
        /// <param name="indentLevel">The indention level.</param>
        /// <param name="offset">Optional indention level offset.</param>
        protected internal void DrawIndentedPropertyField(SerializedProperty property, int indentLevel, int offset = k_IndentOffset)
        {
            var originalWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = originalWidth - (indentLevel * offset);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(indentLevel * offset, false);
            EditorGUILayout.PropertyField(property, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = originalWidth;
        }

        /// <summary>
        /// Helper method to draw the properties of the specified child type <typeparamref name="T"/> component within a FoldoutHeaderGroup.
        /// </summary>
        /// <typeparam name="T">The specific child type that should have its properties drawn.</typeparam>
        /// <param name="type">The component type of the <see cref="UnityEditor.Editor.target"/>.</param>
        /// <param name="displayProperties">The <see cref="Action"/> to invoke that will draw the type <typeparamref name="T"/> properties.</param>
        /// <param name="expanded">The <typeparamref name="T"/> current expanded property value</param>
        /// <param name="setExpandedProperty">The <see cref="Action{bool}"/> invoked to apply the updated <paramref name="expanded"/> value.</param>
        protected void DrawFoldOutGroup<T>(Type type, Action displayProperties, bool expanded, Action<bool> setExpandedProperty)
        {
            var baseClass = target as TT;
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            var currentClass = typeof(T);
            if (type.IsSubclassOf(currentClass) || (!type.IsSubclassOf(currentClass) && currentClass.IsSubclassOf(typeof(TT))))
            {
                var expandedValue = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, $"{currentClass.Name} Properties");
                if (expandedValue)
                {
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    displayProperties.Invoke();
                }
                else
                {
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                EditorGUILayout.Space();
                setExpandedProperty.Invoke(expandedValue);
            }
            else
            {
                displayProperties.Invoke();
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}

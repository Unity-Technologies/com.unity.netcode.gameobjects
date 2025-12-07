using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// The base Netcode Editor helper class to display derived <see cref="MonoBehaviour"/> based components <br />
    /// where each child generation's properties will be displayed within a FoldoutHeaderGroup.
    /// </summary>
    /// <remarks>
    /// <see cref="TT"/> Defines the base <see cref="MonoBehaviour"/> derived component type where <see cref="DrawFoldOutGroup"/>'s type T
    /// refers to any child derived class of <see cref="TT"/>. This provides the ability to have multiple child generation derived custom
    /// editos that each child derivation handles drawing its unique properies from that of its parent class.
    /// </remarks>
    /// <typeparam name="TT">The base <see cref="MonoBehaviour"/> derived component type</typeparam>
    [CanEditMultipleObjects]
    public partial class NetcodeEditorBase<TT> : UnityEditor.Editor where TT : MonoBehaviour
    {
        private const int k_IndentOffset = 15;
        private int m_IndentOffset = 0;
        private int m_IndentLevel = 0;
        private float m_OriginalLabelWidth;

        /// <inheritdoc cref="UnityEditor.Editor.OnEnable"/>
        public virtual void OnEnable()
        {
        }

        /// <summary>
        /// Draws a <see cref="SerializedProperty"/> with the option to provide the font style to use.
        /// </summary>
        /// <param name="property">The serialized property (<see cref="SerializedProperty"/>) to draw within the inspector view.</param>
        /// <param name="fontStyle">The font style (<see cref="FontStyle"/>) to use when drawing the label of the property field.</param>
        private protected void DrawPropertyField(SerializedProperty property, FontStyle fontStyle = FontStyle.Normal)
        {
            var originalWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = originalWidth - (m_IndentOffset * m_IndentLevel);
            var originalLabelFontStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = fontStyle;
            EditorGUILayout.PropertyField(property);
            EditorStyles.label.fontStyle = originalLabelFontStyle;
            EditorGUIUtility.labelWidth = originalWidth;
        }

        /// <summary>
        /// Will begin a new indention level for drawing propery fields.
        /// </summary>
        /// <remarks>
        /// You *must* call <see cref="EndIndent"/> when returning back to the previous indention level.<br />
        /// For additional information:<br />
        /// - <see cref="EndIndent"/><br />
        /// - <see cref="DrawPropertyField"/><br />
        /// </remarks>
        /// <param name="offset">(optional) The size in pixels of the offset. If no value passed, then it uses a default of 15 pixels.</param>
        private protected void BeginIndent(int offset = k_IndentOffset)
        {
            m_IndentOffset = offset;
            m_IndentLevel++;
            GUILayout.BeginHorizontal();
            GUILayout.Space(m_IndentOffset);
            GUILayout.BeginVertical();
        }

        /// <summary>
        /// Will end the current indention level when drawing propery fields.
        /// </summary>
        /// <remarks>
        /// For additional information:<br />
        /// - <see cref="BeginIndent"/><br />
        /// - <see cref="DrawPropertyField"/><br />
        /// </remarks>
        private protected void EndIndent()
        {
            if (m_IndentLevel == 0)
            {
                Debug.LogWarning($"Invoking {nameof(EndIndent)} when the indent level is already 0!");
                return;
            }
            m_IndentLevel--;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Helper method to draw the properties of the specified child type <typeparamref name="T"/> component within a FoldoutHeaderGroup.
        /// </summary>
        /// <remarks>
        /// <see cref="T"/> must be a sub-class of the root parent class type <see cref="TT"/>.
        /// </remarks>
        /// <typeparam name="T">The specific child derived type of <see cref="TT"/> or the type of <see cref="TT"/> that should have its properties drawn.</typeparam>
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

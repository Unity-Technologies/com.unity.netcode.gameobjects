#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The custom editor for <see cref="BaseNetcodeExtension"/> derived classes.
/// </summary>
/// <remarks>
/// This custom editor will provide class derived hierarchy fold out group properties
/// without having to add any customized properties.
/// </remarks>
[CustomEditor(typeof(BaseNetcodeExtension), true)]
[CanEditMultipleObjects]
public class BaseNetcodeExtensionEditor : Editor
{
    private Dictionary<Type, List<SerializedProperty>> m_SerializedProperties = new Dictionary<Type, List<SerializedProperty>>();

    /// <inheritdoc/>
    public virtual void OnEnable()
    {
        BuildSerializedProperties();
    }

    /// <summary>
    /// Builds a list of the <see cref="SerializedProperty"/>s to draw in the inspector view.
    /// </summary>
    private void BuildSerializedProperties()
    {
        m_SerializedProperties.Clear();
        var baseNetcodeExtension = target as BaseNetcodeExtension;
        foreach (var entry in baseNetcodeExtension.IsExpandedTable)
        {
            var fields = entry.Key.Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.GetCustomAttribute<HideInInspector>() != null)
                {
                    continue;
                }

                var serializedProperty = serializedObject.FindProperty(fieldInfo.Name);
                if (serializedProperty != null)
                {
                    if (!m_SerializedProperties.ContainsKey(entry.Key.Type))
                    {
                        m_SerializedProperties.Add(entry.Key.Type, new List<SerializedProperty>());
                    }
                    m_SerializedProperties[entry.Key.Type].Add(serializedProperty);
                }
            }
        }
    }

    /// <summary>
    /// Draws the <see cref="SerializedProperty"/>s of the specified <see cref="SerializableType"/>.
    /// </summary>
    /// <param name="type">The <see cref="SerializableType"/> that is generated during <see cref="BaseNetcodeExtension.OnValidate"/></param>
    private void DrawSerializedProperties(SerializableType type)
    {
        // In the event there are no public properties to draw, just exit early.
        if (!m_SerializedProperties.ContainsKey(type.Type))
        {
            return;
        }
        foreach (var serializedProperty in m_SerializedProperties[type.Type])
        {
            EditorGUILayout.PropertyField(serializedProperty);
        }
    }

    /// <summary>
    /// Draws the foldout group and if expanded the associated SerializedProperties of the <see cref="SerializableType"/>.
    /// </summary>
    /// <param name="type">The <see cref="SerializableType"/> that is generated during <see cref="BaseNetcodeExtension.OnValidate"/></param>
    private void DrawFoldoutGroup(SerializableType type)
    {
        var baseNetcodeExtension = target as BaseNetcodeExtension;
        var isExpanded = baseNetcodeExtension.IsExpandedTable[type];

        isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(isExpanded, $"{type.Type.Name} Properties");
        if (baseNetcodeExtension.IsExpandedTable[type])
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (!m_SerializedProperties.ContainsKey(type.Type))
            {
                Debug.LogError($"[{type.Type.Name}] Does not have an entry in the {nameof(m_SerializedProperties)} table!");
            }
            else
            {
                DrawSerializedProperties(type);
            }
        }
        else
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        baseNetcodeExtension.IsExpandedTable[type] = isExpanded;
    }

    /// <inheritdoc/>
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        serializedObject.Update();
        var targetType = target.GetType();
        var baseNetcodeExtension = target as BaseNetcodeExtension;
        var keys = baseNetcodeExtension.IsExpandedTable.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
        {
            var type = keys[i];
            if (!m_SerializedProperties.ContainsKey(type.Type))
            {
                // If the type has no properties then just draw its name
                EditorGUILayout.LabelField($"{type.Type.Name}");
            }
            else // If this is the actual type of the component, then just draw its properties.
            if (type.Type == targetType)
            {
                DrawSerializedProperties(type);
            }
            else
            {
                // Otherwise, any parent class of the target type will be placed within a foldout group.
                DrawFoldoutGroup(type);
            }
            EditorGUILayout.Space();
        }
        serializedObject.ApplyModifiedProperties();
        EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
    }
}
#endif

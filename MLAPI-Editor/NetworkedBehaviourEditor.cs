using MLAPI.Attributes;
using MLAPI.MonoBehaviours.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkedBehaviour), true)]
    [CanEditMultipleObjects]
    public class NetworkedBehaviourInspector : Editor
    {
        private bool initialized;
        protected List<string> syncedVarNames = new List<string>();

        private GUIContent syncedVarLabelGuiContent;

        private void Init(MonoScript script)
        {
            initialized = true;

            syncedVarLabelGuiContent = new GUIContent("SyncedVar", "This variable has been marked with the [SyncedVar] attribute.");

            FieldInfo[] fields = script.GetClass().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                Attribute[] attributes = (Attribute[])fields[i].GetCustomAttributes(typeof(SyncedVar), true);
                if (attributes.Length > 0)
                    syncedVarNames.Add(fields[i].Name);
            }
        }

        public override void OnInspectorGUI()
        {
            if (!initialized)
            {
                serializedObject.Update();
                SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
                if (scriptProperty == null)
                    return;

                MonoScript targetScript = scriptProperty.objectReferenceValue as MonoScript;
                Init(targetScript);
            }

            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            SerializedProperty property = serializedObject.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                bool isSyncVar = syncedVarNames.Contains(property.name);
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (property.name == "m_Script")
                        EditorGUI.BeginDisabledGroup(true);

                    EditorGUILayout.PropertyField(property, true);

                    if (isSyncVar)
                        GUILayout.Label(syncedVarLabelGuiContent, EditorStyles.miniLabel, GUILayout.Width(EditorStyles.miniLabel.CalcSize(syncedVarLabelGuiContent).x));

                    if (property.name == "m_Script")
                        EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(property, true);

                    if (isSyncVar)
                        GUILayout.Label(syncedVarLabelGuiContent, EditorStyles.miniLabel, GUILayout.Width(EditorStyles.miniLabel.CalcSize(syncedVarLabelGuiContent).x));

                    EditorGUILayout.EndHorizontal();
                }
                expanded = false;
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();  
        }
    }
}

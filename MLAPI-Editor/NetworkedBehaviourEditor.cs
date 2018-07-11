using MLAPI;
using MLAPI.Attributes;
using MLAPI.Data;
using MLAPI.MonoBehaviours.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkedBehaviour), true)]
    [CanEditMultipleObjects]
    public class NetworkedBehaviourEditor : Editor
    {
        private bool initialized;
        private HashSet<string> syncedVarNames = new HashSet<string>();
        private List<string> networkedVarNames = new List<string>();
        private Dictionary<string, FieldInfo> networkedVarFields = new Dictionary<string, FieldInfo>();
        private Dictionary<string, object> networkedVarObjects = new Dictionary<string, object>();

        private GUIContent syncedVarLabelGuiContent;
        private GUIContent networkedVarLabelGuiContent;

        private void Init(MonoScript script)
        {
            initialized = true;

            syncedVarLabelGuiContent = new GUIContent("SyncedVar", "This variable has been marked with the [SyncedVar] attribute.");
            networkedVarLabelGuiContent = new GUIContent("[NetworkedVar]", "This variable has been marked with the [SyncedVar] attribute.");

            FieldInfo[] fields = script.GetClass().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                Attribute[] attributes = (Attribute[])fields[i].GetCustomAttributes(typeof(SyncedVar), true);
                if (attributes.Length > 0)
                    syncedVarNames.Add(fields[i].Name);

                Type ft = fields[i].FieldType;
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(NetworkedVar<>))
                {
                    networkedVarNames.Add(fields[i].Name);
                    networkedVarFields.Add(fields[i].Name, fields[i]);
                }
            }
        }

        void RenderNetworkedVar(int index)
        {
            Type type = networkedVarFields[networkedVarNames[index]].GetValue(target).GetType();
            Type genericType = type.GetGenericArguments()[0];

            EditorGUILayout.BeginHorizontal();
            if (genericType == typeof(string))
            {
                NetworkedVar<string> var = (NetworkedVar<string>)networkedVarFields[networkedVarNames[index]].GetValue(target);
                var.Value = EditorGUILayout.TextField(networkedVarNames[index], var.Value);
            }
            else if (genericType.IsValueType)
            {
                MethodInfo method = typeof(NetworkedBehaviourEditor).GetMethod("RenderNetworkedVarValueType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
                MethodInfo genericMethod = method.MakeGenericMethod(genericType);
                genericMethod.Invoke(this, new object[] { (object)index });
            }
            else
            {
                EditorGUILayout.LabelField("Type not renderable");
            }
            GUILayout.Label(networkedVarLabelGuiContent, EditorStyles.miniLabel, GUILayout.Width(EditorStyles.miniLabel.CalcSize(networkedVarLabelGuiContent).x));
            EditorGUILayout.EndHorizontal();
        }

        void RenderNetworkedVarValueType<T>(int index) where T : struct
        {
            NetworkedVar<T> var = (NetworkedVar<T>)networkedVarFields[networkedVarNames[index]].GetValue(target);
            Type type = typeof(T);
            ValueType val = var.Value;
            string name = networkedVarNames[index];
            if (type == typeof(int))
                val = EditorGUILayout.IntField(name, Convert.ToInt32(val));
            else if (type == typeof(uint))
                val = (uint)EditorGUILayout.IntField(name, Convert.ToInt32(val));
            else if (type == typeof(short))
                val = (short)EditorGUILayout.IntField(name, Convert.ToInt32(val));
            else if (type == typeof(ushort))
                val = (ushort)EditorGUILayout.IntField(name, Convert.ToInt32(val));
            else if (type == typeof(sbyte))
                val = (sbyte)EditorGUILayout.IntField(name, Convert.ToInt32(val));
            else if (type == typeof(byte))
                val = (byte)EditorGUILayout.IntField(name, Convert.ToInt32(val));
            else if (type == typeof(long))
                val = EditorGUILayout.LongField(name, Convert.ToInt64(val));
            else if (type == typeof(ulong))
                val = (ulong)EditorGUILayout.LongField(name, Convert.ToInt64(val));
            else if (type == typeof(bool))
                val = EditorGUILayout.Toggle(name, Convert.ToBoolean(val));
            else if (type == typeof(char))
            {
                char[] chars = EditorGUILayout.TextField(name, Convert.ToString(val)).ToCharArray();
                if (chars.Length > 0)
                    val = chars[0];
            }
            // TODO - more value types here
            else
            {
                EditorGUILayout.LabelField("Type not renderable");
            }

            var.Value = (T)val;
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

            for (int i = 0; i < networkedVarNames.Count; i++)
                RenderNetworkedVar(i);

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
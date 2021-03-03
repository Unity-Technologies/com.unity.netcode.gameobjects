using System;
using System.Collections.Generic;
using System.Reflection;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkBehaviour), true)]
    [CanEditMultipleObjects]
    public class NetworkBehaviourEditor : Editor
    {
        private bool initialized;
        private readonly List<string> networkVariableNames = new List<string>();
        private readonly Dictionary<string, FieldInfo> networkVariableFields = new Dictionary<string, FieldInfo>();
        private readonly Dictionary<string, object> networkVariableObjects = new Dictionary<string, object>();

        private GUIContent networkVariableLabelGuiContent;

        private void Init(MonoScript script)
        {
            initialized = true;
            
            networkVariableNames.Clear();
            networkVariableFields.Clear();
            networkVariableObjects.Clear();

            networkVariableLabelGuiContent = new GUIContent("NetworkVariable", "This variable is a NetworkVariable. It can not be serialized and can only be changed during runtime.");

            FieldInfo[] fields = script.GetClass().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            for (int i = 0; i < fields.Length; i++)
            {
                Type ft = fields[i].FieldType;
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(NetworkVariable<>) && !fields[i].IsDefined(typeof(HideInInspector), true))
                {
                    networkVariableNames.Add(fields[i].Name);
                    networkVariableFields.Add(fields[i].Name, fields[i]);
                }
            }
        }

        void RenderNetworkVariable(int index)
        {
            if (!networkVariableFields.ContainsKey(networkVariableNames[index]))
            {
                serializedObject.Update();
                SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
                if (scriptProperty == null)
                    return;

                MonoScript targetScript = scriptProperty.objectReferenceValue as MonoScript;
                Init(targetScript);
            }

            object value = networkVariableFields[networkVariableNames[index]].GetValue(target);
            if (value == null)
            {
                Type fieldType = networkVariableFields[networkVariableNames[index]].FieldType;
                INetworkVariable var = (INetworkVariable) Activator.CreateInstance(fieldType, true);
                networkVariableFields[networkVariableNames[index]].SetValue(target, var);
            }
            
            Type type = networkVariableFields[networkVariableNames[index]].GetValue(target).GetType();
            Type genericType = type.GetGenericArguments()[0];

            EditorGUILayout.BeginHorizontal();
            if (genericType == typeof(string))
            {
                NetworkVariable<string> var = (NetworkVariable<string>)networkVariableFields[networkVariableNames[index]].GetValue(target);
                var.Value = EditorGUILayout.TextField(networkVariableNames[index], var.Value);
            }
            else if (genericType.IsValueType)
            {
                MethodInfo method = typeof(NetworkBehaviourEditor).GetMethod("RenderNetworkVariableValueType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
                MethodInfo genericMethod = method.MakeGenericMethod(genericType);
                genericMethod.Invoke(this, new[] { (object)index });
            }
            else
            {
                EditorGUILayout.LabelField("Type not renderable");
            }
            GUILayout.Label(networkVariableLabelGuiContent, EditorStyles.miniLabel, GUILayout.Width(EditorStyles.miniLabel.CalcSize(networkVariableLabelGuiContent).x));
            EditorGUILayout.EndHorizontal();
        }

        void RenderNetworkVariableValueType<T>(int index) where T : struct
        {
            NetworkVariable<T> var = (NetworkVariable<T>)networkVariableFields[networkVariableNames[index]].GetValue(target);
            Type type = typeof(T);
            object val = var.Value;
            string name = networkVariableNames[index];

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (type == typeof(int))
                    val = EditorGUILayout.IntField(name, (int)val);
                else if (type == typeof(uint))
                    val = (uint)EditorGUILayout.LongField(name, (long)((uint)val));
                else if (type == typeof(short))
                    val = (short)EditorGUILayout.IntField(name, (int)((short)val));
                else if (type == typeof(ushort))
                    val = (ushort)EditorGUILayout.IntField(name, (int)((ushort)val));
                else if (type == typeof(sbyte))
                    val = (sbyte)EditorGUILayout.IntField(name, (int)((sbyte)val));
                else if (type == typeof(byte))
                    val = (byte)EditorGUILayout.IntField(name, (int)((byte)val));
                else if (type == typeof(long))
                    val = EditorGUILayout.LongField(name, (long)val);
                else if (type == typeof(ulong))
                    val = (ulong)EditorGUILayout.LongField(name, (long)((ulong)val));
                else if (type == typeof(bool))
                    val = EditorGUILayout.Toggle(name, (bool)val);
                else if (type == typeof(string))
                    val = EditorGUILayout.TextField(name, (string)val);
                else if (type.IsEnum)
                    val = EditorGUILayout.EnumPopup(name, (Enum) val);
                else
                    EditorGUILayout.LabelField("Type not renderable");

                var.Value = (T)val;
            }
            else
            {
                EditorGUILayout.LabelField(name, EditorStyles.wordWrappedLabel);
                EditorGUILayout.SelectableLabel(val.ToString(), EditorStyles.wordWrappedLabel);
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

            for (int i = 0; i < networkVariableNames.Count; i++)
                RenderNetworkVariable(i);

            SerializedProperty property = serializedObject.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (property.name == "m_Script")
                        EditorGUI.BeginDisabledGroup(true);

                    EditorGUILayout.PropertyField(property, true);
                                   
                    if (property.name == "m_Script")
                        EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(property, true);
                    EditorGUILayout.EndHorizontal();
                }
                expanded = false;
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }
    }
}

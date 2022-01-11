using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Interest;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    [CustomPropertyDrawer(typeof(AssetBasedKernelInstanceProperty))]
    public class AssetBasedKernelInstancePropertyField : PropertyDrawer
    {
        static readonly List<Type> k_AllTypes;
        static readonly string[] k_AllTypeNames;

        static AssetBasedKernelInstancePropertyField()
        {
            k_AllTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    typeof(IInterestKernel<NetworkObject>).IsAssignableFrom(t)
                    && !t.IsAbstract
                    && t.IsSerializable
                    && t.GetConstructor(new Type[0]) != null)
                .ToList();
            k_AllTypes.Sort((a,b) => a.Name.CompareTo(b.Name));
            k_AllTypes.Insert(0, typeof(void));

            k_AllTypeNames = k_AllTypes.Select(t => t.Name).ToArray();
            for (int i = 1; i < k_AllTypes.Count; ++i)
            {
                if (k_AllTypeNames.Count(n => n == k_AllTypeNames[i]) > 1)
                {
                    k_AllTypeNames[i] = k_AllTypeNames[i] + $" ({k_AllTypes[i].Assembly.GetName().Name})";
                }
            }

            k_AllTypeNames[0] = "None";
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int currentId;
            var currentNodeObject = property.managedReferenceValue;
            if (currentNodeObject == null)
            {
                currentId = 0;
            }
            else
            {
                int listIndex = k_AllTypes.IndexOf(currentNodeObject.GetType());
                if (listIndex == -1)
                {
                    currentId = 0;
                }
                else
                {
                    currentId = listIndex;
                }
            }

            var popupRect = position;
            popupRect.height = EditorGUIUtility.singleLineHeight;

            int newIndex = EditorGUI.Popup(popupRect, "Kernel", currentId, k_AllTypeNames);
            if (newIndex != currentId)
            {
                if (newIndex == 0)
                {
                    property.managedReferenceValue = null;
                }
                else
                {
                    var type = k_AllTypes[newIndex];
                    var instance = Activator.CreateInstance(type);
                    property.managedReferenceValue = instance;
                }
            }

            EditorGUI.PropertyField(position, property, null, true);
        }
    }
}

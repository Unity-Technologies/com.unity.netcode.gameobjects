using System;
using System.Collections.Generic;
using Unity.Netcode.Interest;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    public class InterestNodeInspector : EditorWindow
    {
        [MenuItem("NetCode/Interest Node Inspector")]
        static void OpenInterestNodeInspector() => EditorWindow.GetWindow<InterestNodeInspector>();

        [NonSerialized]
        HashSet<object> m_Foldouts = new HashSet<object>();

        private Vector2 m_ScrollPosition;

        private void OnGUI()
        {
            var selection = Selection.activeGameObject;
            if (selection == null)
            {
                return;
            }

            var selectedNetworkManager = selection.GetComponent<NetworkManager>();
            if (selectedNetworkManager == null)
            {
                return;
            }

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            foreach(var node in selectedNetworkManager.InterestManager.GetChildNodes())
            {
                DrawNode(node);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawNode(IInterestNode<NetworkObject> node)
        {
            string nodeName;
            if (node is INamedInterestNode namedNode)
            {
                nodeName = namedNode.Name;
            }
            else
            {
                nodeName = node.GetType().Name;
            }

            if (node is IStatefulInterestNode<NetworkObject> statefulNode)
            {
                bool foldout = m_Foldouts.Contains(node);
                bool newFoldout = EditorGUILayout.Foldout(foldout, nodeName);
                if (newFoldout != foldout)
                {
                    if (newFoldout)
                    {
                        m_Foldouts.Add(node);
                    }
                    else
                    {
                        m_Foldouts.Remove(node);
                    }
                }

                if (newFoldout)
                {
                    EditorGUI.indentLevel++;
                    var objects = new List<NetworkObject>();
                    statefulNode.GetManagedObjects(objects);

                    foreach (var obj in objects)
                    {
                        EditorGUILayout.ObjectField(obj, typeof(NetworkObject), true);
                    }

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.PrefixLabel(nodeName);
            }
        }
    }
}

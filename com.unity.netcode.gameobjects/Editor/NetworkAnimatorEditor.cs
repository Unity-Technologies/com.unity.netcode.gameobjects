using System.Linq;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// The <see cref="CustomEditor"/> for <see cref="NetworkAnimator"/>
    /// </summary>
    [CustomEditor(typeof(NetworkAnimator), true)]
    [CanEditMultipleObjects]
    public class NetworkAnimatorEditor : NetcodeEditorBase<NetworkAnimator>    
    {
        private static float s_MaxRowWidth = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + 5;
        private SerializedProperty m_AuthorityMode;
        private SerializedProperty m_Animator;

        public override void OnEnable()
        {
            m_AuthorityMode = serializedObject.FindProperty(nameof(NetworkAnimator.AuthorityMode));

            m_Animator = serializedObject.FindProperty("m_Animator");
        }

        private void DisplayNetworkAnimatorProperties()
        {
            var networkAnimator = target as NetworkAnimator;
            EditorGUILayout.PropertyField(m_AuthorityMode);
            EditorGUILayout.PropertyField(m_Animator);
            if (networkAnimator.Animator != null)
            {
                EditorGUILayout.Space();
                networkAnimator.AnimatorParametersExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(networkAnimator.AnimatorParametersExpanded, $"Animator Parameters to Synchronize");
                {
                    var parameters = networkAnimator.Animator.parameters;
                    networkAnimator.AnimatorParameterEntryTable.Clear();
                    foreach (var parameterEntry in networkAnimator.AnimatorParameterEntries)
                    {
                        if (!networkAnimator.AnimatorParameterEntryTable.ContainsKey(parameterEntry.NameHash))
                        {
                            networkAnimator.AnimatorParameterEntryTable.Add(parameterEntry.NameHash, parameterEntry);
                        }
                    }
                    if (networkAnimator.AnimatorParametersExpanded)
                    {
                        foreach (var parameter in parameters)
                        {
                            if (!networkAnimator.AnimatorParameterEntryTable.ContainsKey(parameter.nameHash))
                            {
                                networkAnimator.AnimatorParameterEntryTable.Add(parameter.nameHash, new NetworkAnimator.AnimatorParameterEntry(parameter));
                            }
                            var parameterEntry = networkAnimator.AnimatorParameterEntryTable[parameter.nameHash];
                            parameterEntry.Synchronize = EditorGUILayout.ToggleLeft($"{parameterEntry.Name}  ({parameterEntry.ParameterType.ToString()})", parameterEntry.Synchronize);
                            parameterEntry.ParameterType = parameter.type;
                            parameterEntry.Name = parameter.name;
                            parameterEntry.NameHash = parameter.nameHash;
                            networkAnimator.AnimatorParameterEntryTable[parameter.nameHash] = parameterEntry;
                        }
                    }
                    networkAnimator.AnimatorParameterEntries = networkAnimator.AnimatorParameterEntryTable.Values.ToList();
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        public override void OnInspectorGUI()
        {
            var networkAnimator = target as NetworkAnimator;
            void SetExpanded(bool expanded) { networkAnimator.NetworkAnimatorExpanded = expanded; }
            DrawFoldOutGroup<NetworkTransform>(networkAnimator.GetType(), DisplayNetworkAnimatorProperties, networkAnimator.NetworkAnimatorExpanded, SetExpanded);
            base.OnInspectorGUI();
        }
    }
}

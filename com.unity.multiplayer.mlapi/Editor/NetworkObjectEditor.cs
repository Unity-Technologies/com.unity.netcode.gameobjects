using System.Collections.Generic;
using MLAPI;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkObject), true)]
    [CanEditMultipleObjects]
    public class NetworkObjectEditor : Editor
    {
        private bool m_Initialized;
        private NetworkObject m_NetworkObject;
        private bool m_ShowObservers;

        private void Init()
        {
            if (m_Initialized) return;

            m_Initialized = true;
            m_NetworkObject = (NetworkObject)target;
        }

        public override void OnInspectorGUI()
        {
            Init();

            if (!m_NetworkObject.IsSpawned && !ReferenceEquals(NetworkManager.Singleton, null) && NetworkManager.Singleton.IsServer)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Spawn", "Spawns the object across the network"));
                if (GUILayout.Toggle(false, "Spawn", EditorStyles.miniButtonLeft))
                {
                    m_NetworkObject.Spawn();
                    EditorUtility.SetDirty(target);
                }

                EditorGUILayout.EndHorizontal();
            }
            else if (m_NetworkObject.IsSpawned)
            {
                EditorGUILayout.LabelField("PrefabHashGenerator: ", m_NetworkObject.PrefabHashGenerator, EditorStyles.label);
                EditorGUILayout.LabelField("PrefabHash: ", m_NetworkObject.PrefabHash.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("InstanceId: ", m_NetworkObject.NetworkInstanceId.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("NetworkId: ", m_NetworkObject.NetworkObjectId.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("OwnerId: ", m_NetworkObject.OwnerClientId.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsSpawned: ", m_NetworkObject.IsSpawned.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsLocalPlayer: ", m_NetworkObject.IsLocalPlayer.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsOwner: ", m_NetworkObject.IsOwner.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsOwnedByServer: ", m_NetworkObject.IsOwnedByServer.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsPlayerObject: ", m_NetworkObject.IsPlayerObject.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsSceneObject: ", (m_NetworkObject.IsSceneObject == null ? "Null" : m_NetworkObject.IsSceneObject.Value.ToString()), EditorStyles.label);

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    m_ShowObservers = EditorGUILayout.Foldout(m_ShowObservers, "Observers");

                    if (m_ShowObservers)
                    {
                        HashSet<ulong>.Enumerator observerClientIds = m_NetworkObject.GetObservers();

                        EditorGUI.indentLevel += 1;

                        while (observerClientIds.MoveNext())
                        {
                            if (NetworkManager.Singleton.ConnectedClients[observerClientIds.Current].PlayerObject != null)
                            {
                                EditorGUILayout.ObjectField("ClientId: " + observerClientIds.Current, NetworkManager.Singleton.ConnectedClients[observerClientIds.Current].PlayerObject, typeof(GameObject), false);
                            }
                            else
                            {
                                EditorGUILayout.TextField("ClientId: " + observerClientIds.Current, EditorStyles.label);
                            }
                        }

                        EditorGUI.indentLevel -= 1;
                    }
                }
            }
            else
            {
                base.OnInspectorGUI();
                EditorGUILayout.LabelField("PrefabHash: ", m_NetworkObject.PrefabHash.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("InstanceId: ", m_NetworkObject.NetworkInstanceId.ToString(), EditorStyles.label);
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MLAPI.Editor
{
    [CustomEditor(typeof(NetworkObject), true)]
    [CanEditMultipleObjects]
    public class NetworkObjectEditor : UnityEditor.Editor
    {
        private bool m_Initialized;
        private NetworkObject m_NetworkObject;
        private bool m_ShowObservers;

        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            m_Initialized = true;
            m_NetworkObject = (NetworkObject)target;
        }

        public override void OnInspectorGUI()
        {
            Initialize();

            if (EditorApplication.isPlaying && !m_NetworkObject.IsSpawned && m_NetworkObject.NetworkManager != null && m_NetworkObject.NetworkManager.IsServer)
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
            else if (EditorApplication.isPlaying && m_NetworkObject.IsSpawned)
            {
                var guiEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.TextField(nameof(NetworkObject.GlobalObjectIdHash), m_NetworkObject.GlobalObjectIdHash.ToString());
                EditorGUILayout.TextField(nameof(NetworkObject.NetworkObjectId), m_NetworkObject.NetworkObjectId.ToString());
                EditorGUILayout.TextField(nameof(NetworkObject.OwnerClientId), m_NetworkObject.OwnerClientId.ToString());
                EditorGUILayout.Toggle(nameof(NetworkObject.IsSpawned), m_NetworkObject.IsSpawned);
                EditorGUILayout.Toggle(nameof(NetworkObject.IsLocalPlayer), m_NetworkObject.IsLocalPlayer);
                EditorGUILayout.Toggle(nameof(NetworkObject.IsOwner), m_NetworkObject.IsOwner);
                EditorGUILayout.Toggle(nameof(NetworkObject.IsOwnedByServer), m_NetworkObject.IsOwnedByServer);
                EditorGUILayout.Toggle(nameof(NetworkObject.IsPlayerObject), m_NetworkObject.IsPlayerObject);
                if (m_NetworkObject.IsSceneObject.HasValue)
                {
                    EditorGUILayout.Toggle(nameof(NetworkObject.IsSceneObject), m_NetworkObject.IsSceneObject.Value);
                }
                else
                {
                    EditorGUILayout.TextField(nameof(NetworkObject.IsSceneObject), "null");
                }
                EditorGUILayout.Toggle(nameof(NetworkObject.DestroyWithScene), m_NetworkObject.DestroyWithScene);
                EditorGUILayout.TextField(nameof(NetworkObject.NetworkManager), m_NetworkObject.NetworkManager == null ? "null" : m_NetworkObject.NetworkManager.gameObject.name);
                GUI.enabled = guiEnabled;

//kk                if (m_NetworkObject.NetworkManager != null && m_NetworkObject.NetworkManager.IsServer)
//kk                {
//kk                    m_ShowObservers = EditorGUILayout.Foldout(m_ShowObservers, "Observers");
//kk
//kk                    if (m_ShowObservers)
//kk                    {
//kk                        HashSet<ulong>.Enumerator observerClientIds = m_NetworkObject.GetObservers();
//kk
//kk                        EditorGUI.indentLevel += 1;
//kk
//kk                        while (observerClientIds.MoveNext())
//kk                        {
//kk                            if (m_NetworkObject.NetworkManager.ConnectedClients[observerClientIds.Current].PlayerObject != null)
//kk                            {
//kk                                EditorGUILayout.ObjectField($"ClientId: {observerClientIds.Current}", m_NetworkObject.NetworkManager.ConnectedClients[observerClientIds.Current].PlayerObject, typeof(GameObject), false);
//kk                            }
//kk                            else
//kk                            {
//kk                                EditorGUILayout.TextField($"ClientId: {observerClientIds.Current}", EditorStyles.label);
//kk                            }
//kk                        }
//kk
//kk                        EditorGUI.indentLevel -= 1;
//kk                    }
//kk                }
            }
            else
            {
                base.OnInspectorGUI();

                var guiEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.TextField(nameof(NetworkObject.GlobalObjectIdHash), m_NetworkObject.GlobalObjectIdHash.ToString());
                EditorGUILayout.TextField(nameof(NetworkObject.NetworkManager), m_NetworkObject.NetworkManager == null ? "null" : m_NetworkObject.NetworkManager.gameObject.name);
                GUI.enabled = guiEnabled;
            }
        }
    }
}

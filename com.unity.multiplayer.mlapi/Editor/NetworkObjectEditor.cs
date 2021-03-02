using System.Collections.Generic;
using MLAPI;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkObject), true)]
    [CanEditMultipleObjects]
    public class NetworkObjectEditor : Editor
    {
        private bool initialized;
        private NetworkObject networkObject;
        private bool showObservers;

        private void Init()
        {
            if (initialized)
                return;
            initialized = true;
            networkObject = (NetworkObject)target;
        }

        public override void OnInspectorGUI()
        {
            Init();

            if (!networkObject.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Spawn", "Spawns the object across the network"));
                if (GUILayout.Toggle(false, "Spawn", EditorStyles.miniButtonLeft))
                {
                    networkObject.Spawn();
                    EditorUtility.SetDirty(target);
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (networkObject.IsSpawned)
            {
                EditorGUILayout.LabelField("PrefabHashGenerator: ", networkObject.PrefabHashGenerator, EditorStyles.label);
                EditorGUILayout.LabelField("PrefabHash: ", networkObject.PrefabHash.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("InstanceId: ", networkObject.NetworkInstanceId.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("NetworkId: ", networkObject.ObjectId.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("OwnerId: ", networkObject.OwnerClientId.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsSpawned: ", networkObject.IsSpawned.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsLocalPlayer: ", networkObject.IsLocalPlayer.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsOwner: ", networkObject.IsOwner.ToString(), EditorStyles.label);
				EditorGUILayout.LabelField("IsOwnedByServer: ", networkObject.IsOwnedByServer.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsPlayerObject: ", networkObject.IsPlayerObject.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("IsSceneObject: ", (networkObject.IsSceneObject == null ? "Null" : networkObject.IsSceneObject.Value.ToString()), EditorStyles.label);

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    showObservers = EditorGUILayout.Foldout(showObservers, "Observers");

                    if (showObservers)
                    {
                        HashSet<ulong>.Enumerator observerClientIds = networkObject.GetObservers();

                        EditorGUI.indentLevel += 1;

                        while (observerClientIds.MoveNext())
                        {
                            if (NetworkManager.Singleton.ConnectedClients[observerClientIds.Current].PlayerObject != null)
                                EditorGUILayout.ObjectField("ClientId: " + observerClientIds.Current, NetworkManager.Singleton.ConnectedClients[observerClientIds.Current].PlayerObject, typeof(GameObject), false);
                            else
                                EditorGUILayout.TextField("ClientId: " + observerClientIds.Current, EditorStyles.label);
                        }

                        EditorGUI.indentLevel -= 1;
                    }
                }
            }
            else
            {
                base.OnInspectorGUI();
                EditorGUILayout.LabelField("PrefabHash: ", networkObject.PrefabHash.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("InstanceId: ", networkObject.NetworkInstanceId.ToString(), EditorStyles.label);
            }
        }
    }
}

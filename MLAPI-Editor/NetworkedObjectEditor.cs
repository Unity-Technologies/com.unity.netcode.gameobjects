using MLAPI.MonoBehaviours.Core;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkedObject), true)]
    [CanEditMultipleObjects]
    public class NetworkedObjectEditor : Editor
    {
        private bool initialized;
        private NetworkedObject networkedObject;

        private void Init()
        {
            if (initialized)
                return;
            initialized = true;
            networkedObject = (NetworkedObject)target;
        }

        public override void OnInspectorGUI()
        {
            Init();
            if (NetworkingManager.singleton == null || (!NetworkingManager.singleton.isServer && !NetworkingManager.singleton.isClient))
                base.OnInspectorGUI(); //Only run this if we are NOT running server. This is where the ServerOnly box is drawn

            if (!networkedObject.isSpawned && NetworkingManager.singleton != null && NetworkingManager.singleton.isServer)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Spawn", "Spawns the object across the network"));
                if (GUILayout.Toggle(false, "Spawn", EditorStyles.miniButtonLeft))
                {
                    networkedObject.Spawn();
                    EditorUtility.SetDirty(target);
                }
                EditorGUILayout.EndHorizontal();
            }
            else if(networkedObject.isSpawned)
            {
                EditorGUILayout.LabelField("NetworkId: ", networkedObject.NetworkId.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("OwnerId: ", networkedObject.OwnerClientId.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("isSpawned: ", networkedObject.isSpawned.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("isLocalPlayer: ", networkedObject.isLocalPlayer.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("isOwner: ", networkedObject.isOwner.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("isPoolObject: ", networkedObject.isPlayerObject.ToString(), EditorStyles.label);
                EditorGUILayout.LabelField("isPlayerObject: ", networkedObject.isPlayerObject.ToString(), EditorStyles.label);
                //EditorGUILayout.LabelField("ServerOnly: ", networkedObject.ServerOnly.ToString(), EditorStyles.label);
            }
        }
    }
}

using MLAPI.MonoBehaviours.Core;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

[CustomEditor(typeof(NetworkingManager), true)]
[CanEditMultipleObjects]
public class NetworkingManagerEditor : Editor
{
    private ReorderableList networkedObjectList;

    private NetworkingManager networkingManager;
    private bool initialized;

    private void Init()
    {
        if (initialized)
            return;
        initialized = true;
        networkingManager = (NetworkingManager)target;
    }

    private void OnEnable()
    {
        networkedObjectList = new ReorderableList(serializedObject, serializedObject.FindProperty("NetworkConfig").FindPropertyRelative("NetworkedPrefabs"), true, true, true, true);
        networkedObjectList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = networkedObjectList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 30, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("prefab"), GUIContent.none);
            EditorGUI.PropertyField(new Rect(rect.x + rect.width - 30, rect.y, 30, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("playerPrefab"), GUIContent.none);
        };

        networkedObjectList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Networked Prefabs");
        };
    }

    public override void OnInspectorGUI()
    {
        Init();
        if (!networkingManager.isServer && !networkingManager.isClient)
        {
            EditorGUILayout.Space();
            serializedObject.Update();
            networkedObjectList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI(); //Only draw if we don't have a running client or server
        }
        else
        {
            string instanceType = "";
            if (networkingManager.isHost)
                instanceType = "Host";
            else if (networkingManager.isServer)
                instanceType = "Server";
            else if (networkingManager.isClient)
                instanceType = "Client";

            EditorGUILayout.HelpBox("You cannot edit the NetworkConfig when a " + instanceType + " is running", MessageType.Info);
            if (GUILayout.Toggle(false, "Stop " + instanceType, EditorStyles.miniButtonMid))
            {
                if (networkingManager.isHost)
                    networkingManager.StopHost();
                else if (networkingManager.isServer)
                    networkingManager.StopServer();
                else if (networkingManager.isClient)
                    networkingManager.StopClient();
            }
        }
        Repaint();
    }
}

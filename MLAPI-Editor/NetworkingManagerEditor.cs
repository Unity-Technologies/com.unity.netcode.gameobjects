using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using MLAPI;

[CustomEditor(typeof(NetworkingManager), true)]
[CanEditMultipleObjects]
public class NetworkingManagerEditor : Editor
{
    private SerializedProperty DontDestroyOnLoadProperty;
    private SerializedProperty RunInBackgroundProperty;
    private SerializedProperty LogLevelProperty;
    private SerializedProperty NetworkConfigProperty;

    private ReorderableList networkPrefabsList;
    private ReorderableList channelsList;
    private ReorderableList registeredScenesList;

    private NetworkingManager networkingManager;
    private bool initialized;


    private void Init()
    {
        if (initialized)
            return;

        initialized = true;
        networkingManager = (NetworkingManager)target;
        DontDestroyOnLoadProperty = serializedObject.FindProperty("DontDestroy");
        RunInBackgroundProperty = serializedObject.FindProperty("RunInBackground");
        LogLevelProperty = serializedObject.FindProperty("LogLevel");
        NetworkConfigProperty = serializedObject.FindProperty("NetworkConfig");
    }

    private void CheckNullProperties()
    {
        if (DontDestroyOnLoadProperty == null)
            DontDestroyOnLoadProperty = serializedObject.FindProperty("DontDestroy");
        if (RunInBackgroundProperty == null)
            RunInBackgroundProperty = serializedObject.FindProperty("RunInBackground");
        if (LogLevelProperty == null)
            LogLevelProperty = serializedObject.FindProperty("LogLevel");
        if (NetworkConfigProperty == null)
            NetworkConfigProperty = serializedObject.FindProperty("NetworkConfig");
    }

    private void OnEnable()
    {
        networkPrefabsList = new ReorderableList(serializedObject, serializedObject.FindProperty("NetworkConfig").FindPropertyRelative("NetworkedPrefabs"), true, true, true, true);
        networkPrefabsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = networkPrefabsList.serializedProperty.GetArrayElementAtIndex(index);
            int firstLabelWidth = 50;
            int secondLabelWidth = 140;
            float secondFieldWidth = 10;
            int reduceFirstWidth = 45;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, firstLabelWidth, EditorGUIUtility.singleLineHeight), "Prefab");
            EditorGUI.PropertyField(new Rect(rect.x + firstLabelWidth, rect.y, rect.width - firstLabelWidth - secondLabelWidth - secondFieldWidth - reduceFirstWidth,
                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("prefab"), GUIContent.none);

            EditorGUI.LabelField(new Rect(rect.width - secondLabelWidth - secondFieldWidth, rect.y, secondLabelWidth, EditorGUIUtility.singleLineHeight), "Default Player Prefab");
            EditorGUI.PropertyField(new Rect(rect.width - secondFieldWidth, rect.y, secondFieldWidth,
                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("playerPrefab"), GUIContent.none);
        };

        networkPrefabsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "NetworkedPrefabs");
        };

        channelsList = new ReorderableList(serializedObject, serializedObject.FindProperty("NetworkConfig").FindPropertyRelative("Channels"), true, true, true, true);
        channelsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = channelsList.serializedProperty.GetArrayElementAtIndex(index);


            int firstLabelWidth = 50;
            int secondLabelWidth = 40;
            int secondFieldWidth = 150;
            int reduceFirstWidth = 45;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, firstLabelWidth, EditorGUIUtility.singleLineHeight), "Name");
            EditorGUI.PropertyField(new Rect(rect.x + firstLabelWidth, rect.y, rect.width - firstLabelWidth - secondLabelWidth - secondFieldWidth - reduceFirstWidth,
                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Name"), GUIContent.none);


            EditorGUI.LabelField(new Rect(rect.width - secondLabelWidth - secondFieldWidth, rect.y, secondLabelWidth, EditorGUIUtility.singleLineHeight), "Type");
            EditorGUI.PropertyField(new Rect(rect.width - secondFieldWidth, rect.y, secondFieldWidth,
                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Type"), GUIContent.none);
        };

        channelsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Channels");
        };


        registeredScenesList = new ReorderableList(serializedObject, serializedObject.FindProperty("NetworkConfig").FindPropertyRelative("RegisteredScenes"), true, true, true, true);
        registeredScenesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = registeredScenesList.serializedProperty.GetArrayElementAtIndex(index);
            int firstLabelWidth = 50;
            int padding = 20;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, firstLabelWidth, EditorGUIUtility.singleLineHeight), "Name");
            EditorGUI.PropertyField(new Rect(rect.x + firstLabelWidth, rect.y, rect.width - firstLabelWidth - padding,
                EditorGUIUtility.singleLineHeight), element, GUIContent.none);

        };

        registeredScenesList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Registered Scene Names");
        };
    }

    public override void OnInspectorGUI()
    {
        Init();
        CheckNullProperties();
        if (!networkingManager.IsServer && !networkingManager.IsClient)
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(DontDestroyOnLoadProperty);
            EditorGUILayout.PropertyField(RunInBackgroundProperty);
            EditorGUILayout.PropertyField(LogLevelProperty);

            if (networkingManager.NetworkConfig.HandleObjectSpawning)
            {
                EditorGUILayout.Space();
                networkPrefabsList.DoLayoutList();
            }
            EditorGUILayout.Space();
            channelsList.DoLayoutList();
            EditorGUILayout.Space();
            if (networkingManager.NetworkConfig.EnableSceneSwitching)
            {
                registeredScenesList.DoLayoutList();
                EditorGUILayout.Space();
            }

            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
        else
        {
            string instanceType = "";
            if (networkingManager.IsHost)
                instanceType = "Host";
            else if (networkingManager.IsServer)
                instanceType = "Server";
            else if (networkingManager.IsClient)
                instanceType = "Client";

            EditorGUILayout.HelpBox("You cannot edit the NetworkConfig when a " + instanceType + " is running", UnityEditor.MessageType.Info);
            if (GUILayout.Toggle(false, "Stop " + instanceType, EditorStyles.miniButtonMid))
            {
                if (networkingManager.IsHost)
                    networkingManager.StopHost();
                else if (networkingManager.IsServer)
                    networkingManager.StopServer();
                else if (networkingManager.IsClient)
                    networkingManager.StopClient();
            }
        }
        //Repaint();
    }
}

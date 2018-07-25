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
    private ReorderableList messageTypesList;
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
            /*
            SerializedProperty element = networkPrefabsList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 30, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("prefab"), GUIContent.none);
            EditorGUI.PropertyField(new Rect(rect.x + rect.width - 30, rect.y, 30, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("playerPrefab"), GUIContent.none);

            */

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
            EditorGUI.LabelField(rect, "NetworkedPrefabs (Auto Sorted)");
        };

        /*
        messageTypesList = new ReorderableList(serializedObject, serializedObject.FindProperty("NetworkConfig").FindPropertyRelative("MessageTypes"), true, true, true, true);
        messageTypesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = messageTypesList.serializedProperty.GetArrayElementAtIndex(index);


            int firstLabelWidth = 50;
            int secondLabelWidth = networkingManager.NetworkConfig.AllowPassthroughMessages ? 90 : 0;
            float secondFieldWidth = networkingManager.NetworkConfig.AllowPassthroughMessages ? 10 : 0;
            int reduceFirstWidth = networkingManager.NetworkConfig.AllowPassthroughMessages ? 45 : 20;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, firstLabelWidth, EditorGUIUtility.singleLineHeight), "Name");
            EditorGUI.PropertyField(new Rect(rect.x + firstLabelWidth, rect.y, rect.width - firstLabelWidth - secondLabelWidth - secondFieldWidth - reduceFirstWidth,
                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Name"), GUIContent.none);

            if (networkingManager.NetworkConfig.AllowPassthroughMessages)
            {
                EditorGUI.LabelField(new Rect(rect.width - secondLabelWidth - secondFieldWidth, rect.y, secondLabelWidth, EditorGUIUtility.singleLineHeight), "Passthrough");
                EditorGUI.PropertyField(new Rect(rect.width - secondFieldWidth, rect.y, secondFieldWidth,
                    EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Passthrough"), GUIContent.none);
            }
        };

        messageTypesList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "MessageTypes (Auto Sorted)");
        };
        */


        channelsList = new ReorderableList(serializedObject, serializedObject.FindProperty("NetworkConfig").FindPropertyRelative("Channels"), true, true, true, true);
        channelsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = channelsList.serializedProperty.GetArrayElementAtIndex(index);


            int firstLabelWidth = 50;
            int secondLabelWidth = 40;
            int secondFieldWidth = 150;
            int thirdLabelWidth = networkingManager.NetworkConfig.EnableEncryption ? 70 : 0;
            int thirdFieldWidth = networkingManager.NetworkConfig.EnableEncryption ? 10 : 0;
            int reduceFirstWidth = 45;
            int reduceSecondWidth = networkingManager.NetworkConfig.EnableEncryption ? 10 : 0;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, firstLabelWidth, EditorGUIUtility.singleLineHeight), "Name");
            EditorGUI.PropertyField(new Rect(rect.x + firstLabelWidth, rect.y, rect.width - firstLabelWidth - secondLabelWidth - secondFieldWidth - thirdFieldWidth - thirdLabelWidth - reduceFirstWidth,
                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Name"), GUIContent.none);


            EditorGUI.LabelField(new Rect(rect.width - secondLabelWidth - secondFieldWidth - thirdFieldWidth - thirdLabelWidth, rect.y, secondLabelWidth, EditorGUIUtility.singleLineHeight), "Type");
            EditorGUI.PropertyField(new Rect(rect.width - secondFieldWidth - thirdLabelWidth - thirdFieldWidth, rect.y, secondFieldWidth - reduceSecondWidth,
                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Type"), GUIContent.none);

            if (networkingManager.NetworkConfig.EnableEncryption)
            {
                EditorGUI.LabelField(new Rect(rect.width - thirdFieldWidth - thirdLabelWidth, rect.y, thirdLabelWidth, EditorGUIUtility.singleLineHeight), "Encrypted");
                EditorGUI.PropertyField(new Rect(rect.width - thirdFieldWidth, rect.y, secondFieldWidth,
                    EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Encrypted"), GUIContent.none);
            }
        };

        channelsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Channels (Auto Sorted)");
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
            EditorGUI.LabelField(rect, "Registered Scene Names (Auto Sorted)");
        };
    }

    public override void OnInspectorGUI()
    {
        Init();
        CheckNullProperties();
        if (!networkingManager.isServer && !networkingManager.isClient)
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
            messageTypesList.DoLayoutList();
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
            if (networkingManager.isHost)
                instanceType = "Host";
            else if (networkingManager.isServer)
                instanceType = "Server";
            else if (networkingManager.isClient)
                instanceType = "Client";

            EditorGUILayout.HelpBox("You cannot edit the NetworkConfig when a " + instanceType + " is running", UnityEditor.MessageType.Info);
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
        //Repaint();
    }
}

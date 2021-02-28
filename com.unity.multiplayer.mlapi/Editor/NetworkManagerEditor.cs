using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using MLAPI;
using MLAPI.Transports;

[CustomEditor(typeof(NetworkManager), true)]
[CanEditMultipleObjects]
public class NetworkManagerEditor : Editor
{
    // Properties
    private SerializedProperty dontDestroyOnLoadProperty;
    private SerializedProperty runInBackgroundProperty;
    private SerializedProperty logLevelProperty;

    // NetworkConfig
    private SerializedProperty networkConfigProperty;

    // NetworkConfig fields
    private SerializedProperty protocolVersionProperty;
    private SerializedProperty allowRuntimeSceneChangesProperty;
    private SerializedProperty networkTransportProperty;
    private SerializedProperty receiveTickrateProperty;
    private SerializedProperty maxReceiveEventsPerTickRateProperty;
    private SerializedProperty eventTickrateProperty;
    private SerializedProperty maxObjectUpdatesPerTickProperty;
    private SerializedProperty clientConnectionBufferTimeoutProperty;
    private SerializedProperty connectionApprovalProperty;
    private SerializedProperty secondsHistoryProperty;
    private SerializedProperty enableTimeResyncProperty;
    private SerializedProperty timeResyncIntervalProperty;
    private SerializedProperty enableNetworkedVarProperty;
    private SerializedProperty ensureNetworkedVarLengthSafetyProperty;
    private SerializedProperty createPlayerPrefabProperty;
    private SerializedProperty forceSamePrefabsProperty;
    private SerializedProperty usePrefabSyncProperty;
    private SerializedProperty enableSceneManagementProperty;
    private SerializedProperty recycleNetworkIdsProperty;
    private SerializedProperty networkIdRecycleDelayProperty;
    private SerializedProperty rpcHashSizeProperty;
    private SerializedProperty loadSceneTimeOutProperty;
    private SerializedProperty enableMessageBufferingProperty;
    private SerializedProperty messageBufferTimeoutProperty;

    private ReorderableList networkPrefabsList;
    private ReorderableList registeredScenesList;

    private NetworkManager networkManager;
    private bool initialized;

    private readonly List<Type> transportTypes = new List<Type>();
    private string[] transportNames = { "Select transport..." };

    private void ReloadTransports()
    {
        transportTypes.Clear();

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly assembly in assemblies)
        {
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(Transport)))
                {
                    transportTypes.Add(type);
                }
            }
        }

        transportNames = new string[transportTypes.Count + 1];

        transportNames[0] = "Select transport...";

        for (int i = 0; i < transportTypes.Count; i++)
        {
            transportNames[i + 1] = transportTypes[i].Name;
        }
    }

    private void Init()
    {
        if (initialized)
            return;

        initialized = true;
        networkManager = (NetworkManager)target;

        // Base properties
        dontDestroyOnLoadProperty = serializedObject.FindProperty("DontDestroy");
        runInBackgroundProperty = serializedObject.FindProperty("RunInBackground");
        logLevelProperty = serializedObject.FindProperty("LogLevel");
        networkConfigProperty = serializedObject.FindProperty("NetworkConfig");

        // NetworkConfig properties
        protocolVersionProperty = networkConfigProperty.FindPropertyRelative("ProtocolVersion");
        allowRuntimeSceneChangesProperty = networkConfigProperty.FindPropertyRelative("AllowRuntimeSceneChanges");
        networkTransportProperty = networkConfigProperty.FindPropertyRelative("NetworkTransport");
        receiveTickrateProperty = networkConfigProperty.FindPropertyRelative("ReceiveTickrate");
        maxReceiveEventsPerTickRateProperty = networkConfigProperty.FindPropertyRelative("MaxReceiveEventsPerTickRate");
        eventTickrateProperty = networkConfigProperty.FindPropertyRelative("EventTickrate");
        clientConnectionBufferTimeoutProperty = networkConfigProperty.FindPropertyRelative("ClientConnectionBufferTimeout");
        connectionApprovalProperty = networkConfigProperty.FindPropertyRelative("ConnectionApproval");
        secondsHistoryProperty = networkConfigProperty.FindPropertyRelative("SecondsHistory");
        enableTimeResyncProperty = networkConfigProperty.FindPropertyRelative("EnableTimeResync");
        timeResyncIntervalProperty = networkConfigProperty.FindPropertyRelative("TimeResyncInterval");
        enableNetworkedVarProperty = networkConfigProperty.FindPropertyRelative("EnableNetworkedVar");
        ensureNetworkedVarLengthSafetyProperty = networkConfigProperty.FindPropertyRelative("EnsureNetworkedVarLengthSafety");
        createPlayerPrefabProperty = networkConfigProperty.FindPropertyRelative("CreatePlayerPrefab");
        forceSamePrefabsProperty = networkConfigProperty.FindPropertyRelative("ForceSamePrefabs");
        usePrefabSyncProperty = networkConfigProperty.FindPropertyRelative("UsePrefabSync");
        enableSceneManagementProperty = networkConfigProperty.FindPropertyRelative("EnableSceneManagement");
        recycleNetworkIdsProperty = networkConfigProperty.FindPropertyRelative("RecycleNetworkIds");
        networkIdRecycleDelayProperty = networkConfigProperty.FindPropertyRelative("NetworkIdRecycleDelay");
        rpcHashSizeProperty = networkConfigProperty.FindPropertyRelative("RpcHashSize");
        loadSceneTimeOutProperty = networkConfigProperty.FindPropertyRelative("LoadSceneTimeOut");
        enableMessageBufferingProperty = networkConfigProperty.FindPropertyRelative("EnableMessageBuffering");
        messageBufferTimeoutProperty = networkConfigProperty.FindPropertyRelative("MessageBufferTimeout");


        ReloadTransports();
    }

    private void CheckNullProperties()
    {
        // Base properties
        dontDestroyOnLoadProperty = serializedObject.FindProperty("DontDestroy");
        runInBackgroundProperty = serializedObject.FindProperty("RunInBackground");
        logLevelProperty = serializedObject.FindProperty("LogLevel");
        networkConfigProperty = serializedObject.FindProperty("NetworkConfig");

        // NetworkConfig properties
        protocolVersionProperty = networkConfigProperty.FindPropertyRelative("ProtocolVersion");
        allowRuntimeSceneChangesProperty = networkConfigProperty.FindPropertyRelative("AllowRuntimeSceneChanges");
        networkTransportProperty = networkConfigProperty.FindPropertyRelative("NetworkTransport");
        receiveTickrateProperty = networkConfigProperty.FindPropertyRelative("ReceiveTickrate");
        maxReceiveEventsPerTickRateProperty = networkConfigProperty.FindPropertyRelative("MaxReceiveEventsPerTickRate");
        eventTickrateProperty = networkConfigProperty.FindPropertyRelative("EventTickrate");
        clientConnectionBufferTimeoutProperty = networkConfigProperty.FindPropertyRelative("ClientConnectionBufferTimeout");
        connectionApprovalProperty = networkConfigProperty.FindPropertyRelative("ConnectionApproval");
        secondsHistoryProperty = networkConfigProperty.FindPropertyRelative("SecondsHistory");
        enableTimeResyncProperty = networkConfigProperty.FindPropertyRelative("EnableTimeResync");
        timeResyncIntervalProperty = networkConfigProperty.FindPropertyRelative("TimeResyncInterval");
        enableNetworkedVarProperty = networkConfigProperty.FindPropertyRelative("EnableNetworkedVar");
        ensureNetworkedVarLengthSafetyProperty = networkConfigProperty.FindPropertyRelative("EnsureNetworkedVarLengthSafety");
        createPlayerPrefabProperty = networkConfigProperty.FindPropertyRelative("CreatePlayerPrefab");
        forceSamePrefabsProperty = networkConfigProperty.FindPropertyRelative("ForceSamePrefabs");
        usePrefabSyncProperty = networkConfigProperty.FindPropertyRelative("UsePrefabSync");
        enableSceneManagementProperty = networkConfigProperty.FindPropertyRelative("EnableSceneManagement");
        recycleNetworkIdsProperty = networkConfigProperty.FindPropertyRelative("RecycleNetworkIds");
        networkIdRecycleDelayProperty = networkConfigProperty.FindPropertyRelative("NetworkIdRecycleDelay");
        rpcHashSizeProperty = networkConfigProperty.FindPropertyRelative("RpcHashSize");
        loadSceneTimeOutProperty = networkConfigProperty.FindPropertyRelative("LoadSceneTimeOut");
        enableMessageBufferingProperty = networkConfigProperty.FindPropertyRelative("EnableMessageBuffering");
        messageBufferTimeoutProperty = networkConfigProperty.FindPropertyRelative("MessageBufferTimeout");
    }

    private void OnEnable()
    {
        networkPrefabsList = new ReorderableList(serializedObject, serializedObject.FindProperty("NetworkConfig").FindPropertyRelative("NetworkPrefabs"), true, true, true, true);
        networkPrefabsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = networkPrefabsList.serializedProperty.GetArrayElementAtIndex(index);
            int firstLabelWidth = 50;
            int secondLabelWidth = 140;
            float secondFieldWidth = 10;
            int reduceFirstWidth = 45;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, firstLabelWidth, EditorGUIUtility.singleLineHeight), "Prefab");
            EditorGUI.PropertyField(new Rect(rect.x + firstLabelWidth, rect.y, rect.width - firstLabelWidth - secondLabelWidth - secondFieldWidth - reduceFirstWidth,
                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Prefab"), GUIContent.none);

            EditorGUI.LabelField(new Rect(rect.width - secondLabelWidth - secondFieldWidth, rect.y, secondLabelWidth, EditorGUIUtility.singleLineHeight), "Default Player Prefab");

            int playerPrefabIndex = -1;

            for (int i = 0; i < networkManager.NetworkConfig.NetworkPrefabs.Count; i++)
            {
                if (networkManager.NetworkConfig.NetworkPrefabs[i].PlayerPrefab)
                {
                    playerPrefabIndex = i;
                    break;
                }
            }

            using (new EditorGUI.DisabledScope(playerPrefabIndex != -1 && playerPrefabIndex != index))
            {
                EditorGUI.PropertyField(new Rect(rect.width - secondFieldWidth, rect.y, secondFieldWidth,
                    EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("PlayerPrefab"), GUIContent.none);
            }
        };

        networkPrefabsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "NetworkPrefabs");
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

        {
            SerializedProperty iterator = serializedObject.GetIterator();

            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    EditorGUILayout.PropertyField(iterator, false);
                }
            }
        }


        if (!networkManager.IsServer && !networkManager.IsClient)
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(dontDestroyOnLoadProperty);
            EditorGUILayout.PropertyField(runInBackgroundProperty);
            EditorGUILayout.PropertyField(logLevelProperty);

            EditorGUILayout.Space();
            networkPrefabsList.DoLayoutList();

            using (new EditorGUI.DisabledScope(!networkManager.NetworkConfig.EnableSceneManagement))
            {
                registeredScenesList.DoLayoutList();
                EditorGUILayout.Space();
            }


            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(protocolVersionProperty);

            EditorGUILayout.PropertyField(networkTransportProperty);

            if (networkTransportProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("You have no transport selected. A transport is required for the MLAPI to work. Which one do you want?", MessageType.Warning);

                int selection = EditorGUILayout.Popup(0, transportNames);

                if (selection > 0)
                {
                    ReloadTransports();

                    Component transport = networkManager.gameObject.GetComponent(transportTypes[selection - 1]);

                    if (transport == null)
                    {
                        transport = networkManager.gameObject.AddComponent(transportTypes[selection - 1]);
                    }

                    networkTransportProperty.objectReferenceValue = transport;

                    Repaint();
                }
            }

            EditorGUILayout.PropertyField(enableTimeResyncProperty);

            using (new EditorGUI.DisabledScope(!networkManager.NetworkConfig.EnableTimeResync))
            {
                EditorGUILayout.PropertyField(timeResyncIntervalProperty);
            }

            EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(receiveTickrateProperty);
            EditorGUILayout.PropertyField(maxReceiveEventsPerTickRateProperty);
            EditorGUILayout.PropertyField(eventTickrateProperty);
            EditorGUILayout.PropertyField(enableNetworkedVarProperty);

            using (new EditorGUI.DisabledScope(!networkManager.NetworkConfig.EnableNetworkedVar))
            {
                if(maxObjectUpdatesPerTickProperty != null)
                {
                    EditorGUILayout.PropertyField(maxObjectUpdatesPerTickProperty);
                }

                EditorGUILayout.PropertyField(ensureNetworkedVarLengthSafetyProperty);
            }

            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(connectionApprovalProperty);

            using (new EditorGUI.DisabledScope(!networkManager.NetworkConfig.ConnectionApproval))
            {
                EditorGUILayout.PropertyField(clientConnectionBufferTimeoutProperty);
            }

            EditorGUILayout.LabelField("Lag Compensation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(secondsHistoryProperty);

            EditorGUILayout.LabelField("Spawning", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(createPlayerPrefabProperty);
            EditorGUILayout.PropertyField(forceSamePrefabsProperty);

            using (new EditorGUI.DisabledScope(!networkManager.NetworkConfig.EnableSceneManagement))
            {
                bool value = networkManager.NetworkConfig.UsePrefabSync;

                if (!networkManager.NetworkConfig.EnableSceneManagement)
                {
                    usePrefabSyncProperty.boolValue = true;
                }

                EditorGUILayout.PropertyField(usePrefabSyncProperty);

                if (!networkManager.NetworkConfig.EnableSceneManagement)
                {
                    usePrefabSyncProperty.boolValue = value;
                }
            }

            EditorGUILayout.PropertyField(recycleNetworkIdsProperty);

            using (new EditorGUI.DisabledScope(!networkManager.NetworkConfig.RecycleNetworkIds))
            {
                EditorGUILayout.PropertyField(networkIdRecycleDelayProperty);
            }

            EditorGUILayout.PropertyField(enableMessageBufferingProperty);

            using (new EditorGUI.DisabledScope(!networkManager.NetworkConfig.EnableMessageBuffering))
            {
                EditorGUILayout.PropertyField(messageBufferTimeoutProperty);
            }

            EditorGUILayout.LabelField("Bandwidth", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rpcHashSizeProperty);

            EditorGUILayout.LabelField("Scene Management", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableSceneManagementProperty);

            using (new EditorGUI.DisabledScope(!networkManager.NetworkConfig.EnableSceneManagement))
            {
                EditorGUILayout.PropertyField(loadSceneTimeOutProperty);
                EditorGUILayout.PropertyField(allowRuntimeSceneChangesProperty);
            }

            serializedObject.ApplyModifiedProperties();


            // Start buttons below
            {
                string buttonDisabledReasonSuffix = "";

                if (!EditorApplication.isPlaying)
                {
                    buttonDisabledReasonSuffix = ". This can only be done in play mode";
                    GUI.enabled = false;
                }

                if (GUILayout.Button(new GUIContent("Start Host", "Starts a host instance" + buttonDisabledReasonSuffix)))
                {
                    networkManager.StartHost();
                }

                if (GUILayout.Button(new GUIContent("Start Server", "Starts a server instance" + buttonDisabledReasonSuffix)))
                {
                    networkManager.StartServer();
                }

                if (GUILayout.Button(new GUIContent("Start Client", "Starts a client instance" + buttonDisabledReasonSuffix)))
                {
                    networkManager.StartClient();
                }

                if (!EditorApplication.isPlaying)
                {
                    GUI.enabled = true;
                }
            }
        }
        else
        {
            string instanceType = "";

            if (networkManager.IsHost)
                instanceType = "Host";
            else if (networkManager.IsServer)
                instanceType = "Server";
            else if (networkManager.IsClient)
                instanceType = "Client";

            EditorGUILayout.HelpBox("You cannot edit the NetworkConfig when a " + instanceType + " is running.", MessageType.Info);

            if (GUILayout.Button(new GUIContent("Stop " + instanceType, "Stops the " + instanceType + " instance.")))
            {
                if (networkManager.IsHost)
                    networkManager.StopHost();
                else if (networkManager.IsServer)
                    networkManager.StopServer();
                else if (networkManager.IsClient)
                    networkManager.StopClient();
            }
        }
    }
}

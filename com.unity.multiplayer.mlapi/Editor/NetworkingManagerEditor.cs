using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using MLAPI;
using MLAPI.Transports;

[CustomEditor(typeof(NetworkingManager), true)]
[CanEditMultipleObjects]
public class NetworkingManagerEditor : Editor
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
    private SerializedProperty enableEncryptionProperty;
    private SerializedProperty signKeyExchangeProperty;
    private SerializedProperty serverBase64PfxCertificateProperty;

    private ReorderableList networkPrefabsList;
    private ReorderableList registeredScenesList;

    private NetworkingManager networkingManager;
    private bool initialized;

    private readonly List<Type> transportTypes = new List<Type>();
    private string[] transportNames = new string[] { "Select transport..." };

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
        networkingManager = (NetworkingManager)target;

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
        maxObjectUpdatesPerTickProperty = networkConfigProperty.FindPropertyRelative("MaxObjectUpdatesPerTick");
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
        enableEncryptionProperty = networkConfigProperty.FindPropertyRelative("EnableEncryption");
        signKeyExchangeProperty = networkConfigProperty.FindPropertyRelative("SignKeyExchange");
        serverBase64PfxCertificateProperty = networkConfigProperty.FindPropertyRelative("ServerBase64PfxCertificate");


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
        maxObjectUpdatesPerTickProperty = networkConfigProperty.FindPropertyRelative("MaxObjectUpdatesPerTick");
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
        enableEncryptionProperty = networkConfigProperty.FindPropertyRelative("EnableEncryption");
        signKeyExchangeProperty = networkConfigProperty.FindPropertyRelative("SignKeyExchange");
        serverBase64PfxCertificateProperty = networkConfigProperty.FindPropertyRelative("ServerBase64PfxCertificate");
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
                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Prefab"), GUIContent.none);

            EditorGUI.LabelField(new Rect(rect.width - secondLabelWidth - secondFieldWidth, rect.y, secondLabelWidth, EditorGUIUtility.singleLineHeight), "Default Player Prefab");

            int playerPrefabIndex = -1;

            for (int i = 0; i < networkingManager.NetworkConfig.NetworkedPrefabs.Count; i++)
            {
                if (networkingManager.NetworkConfig.NetworkedPrefabs[i].PlayerPrefab)
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
            EditorGUI.LabelField(rect, "NetworkedPrefabs");
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


        if (!networkingManager.IsServer && !networkingManager.IsClient)
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(dontDestroyOnLoadProperty);
            EditorGUILayout.PropertyField(runInBackgroundProperty);
            EditorGUILayout.PropertyField(logLevelProperty);

            EditorGUILayout.Space();
            networkPrefabsList.DoLayoutList();

            using (new EditorGUI.DisabledScope(!networkingManager.NetworkConfig.EnableSceneManagement))
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

                    Component transport = networkingManager.gameObject.GetComponent(transportTypes[selection - 1]);

                    if (transport == null)
                    {
                        transport = networkingManager.gameObject.AddComponent(transportTypes[selection - 1]);
                    }

                    networkTransportProperty.objectReferenceValue = transport;

                    Repaint();
                }
            }

            EditorGUILayout.PropertyField(enableTimeResyncProperty);

            using (new EditorGUI.DisabledScope(!networkingManager.NetworkConfig.EnableTimeResync))
            {
                EditorGUILayout.PropertyField(timeResyncIntervalProperty);
            }

            EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(receiveTickrateProperty);
            EditorGUILayout.PropertyField(maxReceiveEventsPerTickRateProperty);
            EditorGUILayout.PropertyField(eventTickrateProperty);
            EditorGUILayout.PropertyField(enableNetworkedVarProperty);

            using (new EditorGUI.DisabledScope(!networkingManager.NetworkConfig.EnableNetworkedVar))
            {
                EditorGUILayout.PropertyField(maxObjectUpdatesPerTickProperty);
                EditorGUILayout.PropertyField(ensureNetworkedVarLengthSafetyProperty);
            }

            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(connectionApprovalProperty);

            using (new EditorGUI.DisabledScope(!networkingManager.NetworkConfig.ConnectionApproval))
            {
                EditorGUILayout.PropertyField(clientConnectionBufferTimeoutProperty);
            }

            EditorGUILayout.LabelField("Lag Compensation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(secondsHistoryProperty);

            EditorGUILayout.LabelField("Spawning", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(createPlayerPrefabProperty);
            EditorGUILayout.PropertyField(forceSamePrefabsProperty);

            using (new EditorGUI.DisabledScope(!networkingManager.NetworkConfig.EnableSceneManagement))
            {
                bool value = networkingManager.NetworkConfig.UsePrefabSync;

                if (!networkingManager.NetworkConfig.EnableSceneManagement)
                {
                    usePrefabSyncProperty.boolValue = true;
                }

                EditorGUILayout.PropertyField(usePrefabSyncProperty);

                if (!networkingManager.NetworkConfig.EnableSceneManagement)
                {
                    usePrefabSyncProperty.boolValue = value;
                }
            }

            EditorGUILayout.PropertyField(recycleNetworkIdsProperty);

            using (new EditorGUI.DisabledScope(!networkingManager.NetworkConfig.RecycleNetworkIds))
            {
                EditorGUILayout.PropertyField(networkIdRecycleDelayProperty);
            }

            EditorGUILayout.PropertyField(enableMessageBufferingProperty);

            using (new EditorGUI.DisabledScope(!networkingManager.NetworkConfig.EnableMessageBuffering))
            {
                EditorGUILayout.PropertyField(messageBufferTimeoutProperty);
            }

            EditorGUILayout.LabelField("Bandwidth", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rpcHashSizeProperty);

            EditorGUILayout.LabelField("Scene Management", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableSceneManagementProperty);

            using (new EditorGUI.DisabledScope(!networkingManager.NetworkConfig.EnableSceneManagement))
            {
                EditorGUILayout.PropertyField(loadSceneTimeOutProperty);
                EditorGUILayout.PropertyField(allowRuntimeSceneChangesProperty);
            }

            EditorGUILayout.LabelField("Cryptography", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableEncryptionProperty);

            using (new EditorGUI.DisabledScope(!networkingManager.NetworkConfig.EnableEncryption))
            {
                EditorGUILayout.PropertyField(signKeyExchangeProperty);
                EditorGUILayout.PropertyField(serverBase64PfxCertificateProperty);
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
                    networkingManager.StartHost();
                }

                if (GUILayout.Button(new GUIContent("Start Server", "Starts a server instance" + buttonDisabledReasonSuffix)))
                {
                    networkingManager.StartServer();
                }

                if (GUILayout.Button(new GUIContent("Start Client", "Starts a client instance" + buttonDisabledReasonSuffix)))
                {
                    networkingManager.StartClient();
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

            if (networkingManager.IsHost)
                instanceType = "Host";
            else if (networkingManager.IsServer)
                instanceType = "Server";
            else if (networkingManager.IsClient)
                instanceType = "Client";

            EditorGUILayout.HelpBox("You cannot edit the NetworkConfig when a " + instanceType + " is running.", MessageType.Info);

            if (GUILayout.Button(new GUIContent("Stop " + instanceType, "Stops the " + instanceType + " instance.")))
            {
                if (networkingManager.IsHost)
                    networkingManager.StopHost();
                else if (networkingManager.IsServer)
                    networkingManager.StopServer();
                else if (networkingManager.IsClient)
                    networkingManager.StopClient();
            }
        }
    }
}

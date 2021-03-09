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
    private SerializedProperty m_DontDestroyOnLoadProperty;
    private SerializedProperty m_RunInBackgroundProperty;
    private SerializedProperty m_LogLevelProperty;

    // NetworkConfig
    private SerializedProperty m_NetworkConfigProperty;

    // NetworkConfig fields
    private SerializedProperty m_ProtocolVersionProperty;
    private SerializedProperty m_AllowRuntimeSceneChangesProperty;
    private SerializedProperty m_NetworkTransportProperty;
    private SerializedProperty m_ReceiveTickrateProperty;
    private SerializedProperty m_NetworkTickIntervalSecProperty;
    private SerializedProperty m_MaxReceiveEventsPerTickRateProperty;
    private SerializedProperty m_EventTickrateProperty;
    private SerializedProperty m_MaxObjectUpdatesPerTickProperty;
    private SerializedProperty m_ClientConnectionBufferTimeoutProperty;
    private SerializedProperty m_ConnectionApprovalProperty;
    private SerializedProperty m_SecondsHistoryProperty;
    private SerializedProperty m_EnableTimeResyncProperty;
    private SerializedProperty m_TimeResyncIntervalProperty;
    private SerializedProperty m_EnableNetworkVariableProperty;
    private SerializedProperty m_EnsureNetworkVariableLengthSafetyProperty;
    private SerializedProperty m_CreatePlayerPrefabProperty;
    private SerializedProperty m_ForceSamePrefabsProperty;
    private SerializedProperty m_UsePrefabSyncProperty;
    private SerializedProperty m_EnableSceneManagementProperty;
    private SerializedProperty m_RecycleNetworkIdsProperty;
    private SerializedProperty m_NetworkIdRecycleDelayProperty;
    private SerializedProperty m_RpcHashSizeProperty;
    private SerializedProperty m_LoadSceneTimeOutProperty;
    private SerializedProperty m_EnableMessageBufferingProperty;
    private SerializedProperty m_MessageBufferTimeoutProperty;

    private ReorderableList m_NetworkPrefabsList;
    private ReorderableList m_RegisteredScenesList;

    private NetworkManager m_NetworkManager;
    private bool m_Initialized;

    private readonly List<Type> m_TransportTypes = new List<Type>();
    private string[] m_TransportNames = { "Select transport..." };

    private void ReloadTransports()
    {
        m_TransportTypes.Clear();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                if (type.IsSubclassOf(typeof(NetworkTransport)))
                {
                    m_TransportTypes.Add(type);
                }
            }
        }

        m_TransportNames = new string[m_TransportTypes.Count + 1];
        m_TransportNames[0] = "Select transport...";

        for (int i = 0; i < m_TransportTypes.Count; i++)
        {
            m_TransportNames[i + 1] = m_TransportTypes[i].Name;
        }
    }

    private void Init()
    {
        if (m_Initialized) return;

        m_Initialized = true;
        m_NetworkManager = (NetworkManager)target;

        // Base properties
        m_DontDestroyOnLoadProperty = serializedObject.FindProperty("DontDestroy");
        m_RunInBackgroundProperty = serializedObject.FindProperty("RunInBackground");
        m_LogLevelProperty = serializedObject.FindProperty("LogLevel");
        m_NetworkConfigProperty = serializedObject.FindProperty("NetworkConfig");

        // NetworkConfig properties
        m_ProtocolVersionProperty = m_NetworkConfigProperty.FindPropertyRelative("ProtocolVersion");
        m_AllowRuntimeSceneChangesProperty = m_NetworkConfigProperty.FindPropertyRelative("AllowRuntimeSceneChanges");
        m_NetworkTransportProperty = m_NetworkConfigProperty.FindPropertyRelative("NetworkTransport");
        m_ReceiveTickrateProperty = m_NetworkConfigProperty.FindPropertyRelative("ReceiveTickrate");
        m_NetworkTickIntervalSecProperty = m_NetworkConfigProperty.FindPropertyRelative("NetworkTickIntervalSec");
        m_MaxReceiveEventsPerTickRateProperty = m_NetworkConfigProperty.FindPropertyRelative("MaxReceiveEventsPerTickRate");
        m_EventTickrateProperty = m_NetworkConfigProperty.FindPropertyRelative("EventTickrate");
        m_ClientConnectionBufferTimeoutProperty = m_NetworkConfigProperty.FindPropertyRelative("ClientConnectionBufferTimeout");
        m_ConnectionApprovalProperty = m_NetworkConfigProperty.FindPropertyRelative("ConnectionApproval");
        m_SecondsHistoryProperty = m_NetworkConfigProperty.FindPropertyRelative("SecondsHistory");
        m_EnableTimeResyncProperty = m_NetworkConfigProperty.FindPropertyRelative("EnableTimeResync");
        m_TimeResyncIntervalProperty = m_NetworkConfigProperty.FindPropertyRelative("TimeResyncInterval");
        m_EnableNetworkVariableProperty = m_NetworkConfigProperty.FindPropertyRelative("EnableNetworkVariable");
        m_EnsureNetworkVariableLengthSafetyProperty = m_NetworkConfigProperty.FindPropertyRelative("EnsureNetworkVariableLengthSafety");
        m_CreatePlayerPrefabProperty = m_NetworkConfigProperty.FindPropertyRelative("CreatePlayerPrefab");
        m_ForceSamePrefabsProperty = m_NetworkConfigProperty.FindPropertyRelative("ForceSamePrefabs");
        m_UsePrefabSyncProperty = m_NetworkConfigProperty.FindPropertyRelative("UsePrefabSync");
        m_EnableSceneManagementProperty = m_NetworkConfigProperty.FindPropertyRelative("EnableSceneManagement");
        m_RecycleNetworkIdsProperty = m_NetworkConfigProperty.FindPropertyRelative("RecycleNetworkIds");
        m_NetworkIdRecycleDelayProperty = m_NetworkConfigProperty.FindPropertyRelative("NetworkIdRecycleDelay");
        m_RpcHashSizeProperty = m_NetworkConfigProperty.FindPropertyRelative("RpcHashSize");
        m_LoadSceneTimeOutProperty = m_NetworkConfigProperty.FindPropertyRelative("LoadSceneTimeOut");
        m_EnableMessageBufferingProperty = m_NetworkConfigProperty.FindPropertyRelative("EnableMessageBuffering");
        m_MessageBufferTimeoutProperty = m_NetworkConfigProperty.FindPropertyRelative("MessageBufferTimeout");


        ReloadTransports();
    }

    private void CheckNullProperties()
    {
        // Base properties
        m_DontDestroyOnLoadProperty = serializedObject.FindProperty("DontDestroy");
        m_RunInBackgroundProperty = serializedObject.FindProperty("RunInBackground");
        m_LogLevelProperty = serializedObject.FindProperty("LogLevel");
        m_NetworkConfigProperty = serializedObject.FindProperty("NetworkConfig");

        // NetworkConfig properties
        m_ProtocolVersionProperty = m_NetworkConfigProperty.FindPropertyRelative("ProtocolVersion");
        m_AllowRuntimeSceneChangesProperty = m_NetworkConfigProperty.FindPropertyRelative("AllowRuntimeSceneChanges");
        m_NetworkTransportProperty = m_NetworkConfigProperty.FindPropertyRelative("NetworkTransport");
        m_ReceiveTickrateProperty = m_NetworkConfigProperty.FindPropertyRelative("ReceiveTickrate");
        m_NetworkTickIntervalSecProperty = m_NetworkConfigProperty.FindPropertyRelative("NetworkTickIntervalSec");
        m_MaxReceiveEventsPerTickRateProperty = m_NetworkConfigProperty.FindPropertyRelative("MaxReceiveEventsPerTickRate");
        m_EventTickrateProperty = m_NetworkConfigProperty.FindPropertyRelative("EventTickrate");
        m_ClientConnectionBufferTimeoutProperty = m_NetworkConfigProperty.FindPropertyRelative("ClientConnectionBufferTimeout");
        m_ConnectionApprovalProperty = m_NetworkConfigProperty.FindPropertyRelative("ConnectionApproval");
        m_SecondsHistoryProperty = m_NetworkConfigProperty.FindPropertyRelative("SecondsHistory");
        m_EnableTimeResyncProperty = m_NetworkConfigProperty.FindPropertyRelative("EnableTimeResync");
        m_TimeResyncIntervalProperty = m_NetworkConfigProperty.FindPropertyRelative("TimeResyncInterval");
        m_EnableNetworkVariableProperty = m_NetworkConfigProperty.FindPropertyRelative("EnableNetworkVariable");
        m_EnsureNetworkVariableLengthSafetyProperty = m_NetworkConfigProperty.FindPropertyRelative("EnsureNetworkVariableLengthSafety");
        m_CreatePlayerPrefabProperty = m_NetworkConfigProperty.FindPropertyRelative("CreatePlayerPrefab");
        m_ForceSamePrefabsProperty = m_NetworkConfigProperty.FindPropertyRelative("ForceSamePrefabs");
        m_UsePrefabSyncProperty = m_NetworkConfigProperty.FindPropertyRelative("UsePrefabSync");
        m_EnableSceneManagementProperty = m_NetworkConfigProperty.FindPropertyRelative("EnableSceneManagement");
        m_RecycleNetworkIdsProperty = m_NetworkConfigProperty.FindPropertyRelative("RecycleNetworkIds");
        m_NetworkIdRecycleDelayProperty = m_NetworkConfigProperty.FindPropertyRelative("NetworkIdRecycleDelay");
        m_RpcHashSizeProperty = m_NetworkConfigProperty.FindPropertyRelative("RpcHashSize");
        m_LoadSceneTimeOutProperty = m_NetworkConfigProperty.FindPropertyRelative("LoadSceneTimeOut");
        m_EnableMessageBufferingProperty = m_NetworkConfigProperty.FindPropertyRelative("EnableMessageBuffering");
        m_MessageBufferTimeoutProperty = m_NetworkConfigProperty.FindPropertyRelative("MessageBufferTimeout");
    }

    private void OnEnable()
    {
        m_NetworkPrefabsList = new ReorderableList(serializedObject, serializedObject.FindProperty("NetworkConfig").FindPropertyRelative("NetworkPrefabs"), true, true, true, true);
        m_NetworkPrefabsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = m_NetworkPrefabsList.serializedProperty.GetArrayElementAtIndex(index);
            int firstLabelWidth = 50;
            int secondLabelWidth = 140;
            float secondFieldWidth = 10;
            int reduceFirstWidth = 45;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, firstLabelWidth, EditorGUIUtility.singleLineHeight), "Prefab");
            EditorGUI.PropertyField(new Rect(rect.x + firstLabelWidth, rect.y, rect.width - firstLabelWidth - secondLabelWidth - secondFieldWidth - reduceFirstWidth,
                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Prefab"), GUIContent.none);

            EditorGUI.LabelField(new Rect(rect.width - secondLabelWidth - secondFieldWidth, rect.y, secondLabelWidth, EditorGUIUtility.singleLineHeight), "Default Player Prefab");

            int playerPrefabIndex = -1;

            for (int i = 0; i < m_NetworkManager.NetworkConfig.NetworkPrefabs.Count; i++)
            {
                if (m_NetworkManager.NetworkConfig.NetworkPrefabs[i].PlayerPrefab)
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

        m_NetworkPrefabsList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "NetworkPrefabs"); };


        m_RegisteredScenesList = new ReorderableList(serializedObject, serializedObject.FindProperty("NetworkConfig").FindPropertyRelative("RegisteredScenes"), true, true, true, true);
        m_RegisteredScenesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = m_RegisteredScenesList.serializedProperty.GetArrayElementAtIndex(index);
            int firstLabelWidth = 50;
            int padding = 20;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, firstLabelWidth, EditorGUIUtility.singleLineHeight), "Name");
            EditorGUI.PropertyField(new Rect(rect.x + firstLabelWidth, rect.y, rect.width - firstLabelWidth - padding,
                EditorGUIUtility.singleLineHeight), element, GUIContent.none);
        };

        m_RegisteredScenesList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Registered Scene Names"); };
    }

    public override void OnInspectorGUI()
    {
        Init();
        CheckNullProperties();

        {
            var iterator = serializedObject.GetIterator();

            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    EditorGUILayout.PropertyField(iterator, false);
                }
            }
        }


        if (!m_NetworkManager.IsServer && !m_NetworkManager.IsClient)
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_DontDestroyOnLoadProperty);
            EditorGUILayout.PropertyField(m_RunInBackgroundProperty);
            EditorGUILayout.PropertyField(m_LogLevelProperty);

            EditorGUILayout.Space();
            m_NetworkPrefabsList.DoLayoutList();

            using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.EnableSceneManagement))
            {
                m_RegisteredScenesList.DoLayoutList();
                EditorGUILayout.Space();
            }


            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ProtocolVersionProperty);

            EditorGUILayout.PropertyField(m_NetworkTransportProperty);

            if (m_NetworkTransportProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("You have no transport selected. A transport is required for the MLAPI to work. Which one do you want?", MessageType.Warning);

                int selection = EditorGUILayout.Popup(0, m_TransportNames);

                if (selection > 0)
                {
                    ReloadTransports();

                    var transportComponent = m_NetworkManager.gameObject.GetComponent(m_TransportTypes[selection - 1]);

                    if (ReferenceEquals(transportComponent, null))
                    {
                        transportComponent = m_NetworkManager.gameObject.AddComponent(m_TransportTypes[selection - 1]);
                    }

                    m_NetworkTransportProperty.objectReferenceValue = transportComponent;

                    Repaint();
                }
            }

            EditorGUILayout.PropertyField(m_EnableTimeResyncProperty);

            using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.EnableTimeResync))
            {
                EditorGUILayout.PropertyField(m_TimeResyncIntervalProperty);
            }

            EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ReceiveTickrateProperty);
            EditorGUILayout.PropertyField(m_NetworkTickIntervalSecProperty);
            EditorGUILayout.PropertyField(m_MaxReceiveEventsPerTickRateProperty);
            EditorGUILayout.PropertyField(m_EventTickrateProperty);
            EditorGUILayout.PropertyField(m_EnableNetworkVariableProperty);

            using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.EnableNetworkVariable))
            {
                if (m_MaxObjectUpdatesPerTickProperty != null)
                {
                    EditorGUILayout.PropertyField(m_MaxObjectUpdatesPerTickProperty);
                }

                EditorGUILayout.PropertyField(m_EnsureNetworkVariableLengthSafetyProperty);
            }

            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ConnectionApprovalProperty);

            using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.ConnectionApproval))
            {
                EditorGUILayout.PropertyField(m_ClientConnectionBufferTimeoutProperty);
            }

            EditorGUILayout.LabelField("Lag Compensation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SecondsHistoryProperty);

            EditorGUILayout.LabelField("Spawning", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_CreatePlayerPrefabProperty);
            EditorGUILayout.PropertyField(m_ForceSamePrefabsProperty);

            using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.EnableSceneManagement))
            {
                bool value = m_NetworkManager.NetworkConfig.UsePrefabSync;

                if (!m_NetworkManager.NetworkConfig.EnableSceneManagement)
                {
                    m_UsePrefabSyncProperty.boolValue = true;
                }

                EditorGUILayout.PropertyField(m_UsePrefabSyncProperty);

                if (!m_NetworkManager.NetworkConfig.EnableSceneManagement)
                {
                    m_UsePrefabSyncProperty.boolValue = value;
                }
            }

            EditorGUILayout.PropertyField(m_RecycleNetworkIdsProperty);

            using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.RecycleNetworkIds))
            {
                EditorGUILayout.PropertyField(m_NetworkIdRecycleDelayProperty);
            }

            EditorGUILayout.PropertyField(m_EnableMessageBufferingProperty);

            using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.EnableMessageBuffering))
            {
                EditorGUILayout.PropertyField(m_MessageBufferTimeoutProperty);
            }

            EditorGUILayout.LabelField("Bandwidth", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_RpcHashSizeProperty);

            EditorGUILayout.LabelField("Scene Management", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_EnableSceneManagementProperty);

            using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.EnableSceneManagement))
            {
                EditorGUILayout.PropertyField(m_LoadSceneTimeOutProperty);
                EditorGUILayout.PropertyField(m_AllowRuntimeSceneChangesProperty);
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
                    m_NetworkManager.StartHost();
                }

                if (GUILayout.Button(new GUIContent("Start Server", "Starts a server instance" + buttonDisabledReasonSuffix)))
                {
                    m_NetworkManager.StartServer();
                }

                if (GUILayout.Button(new GUIContent("Start Client", "Starts a client instance" + buttonDisabledReasonSuffix)))
                {
                    m_NetworkManager.StartClient();
                }

                if (!EditorApplication.isPlaying)
                {
                    GUI.enabled = true;
                }
            }
        }
        else
        {
            string instanceType = string.Empty;

            if (m_NetworkManager.IsHost) instanceType = "Host";
            else if (m_NetworkManager.IsServer) instanceType = "Server";
            else if (m_NetworkManager.IsClient) instanceType = "Client";

            EditorGUILayout.HelpBox("You cannot edit the NetworkConfig when a " + instanceType + " is running.", MessageType.Info);

            if (GUILayout.Button(new GUIContent("Stop " + instanceType, "Stops the " + instanceType + " instance.")))
            {
                if (m_NetworkManager.IsHost) m_NetworkManager.StopHost();
                else if (m_NetworkManager.IsServer) m_NetworkManager.StopServer();
                else if (m_NetworkManager.IsClient) m_NetworkManager.StopClient();
            }
        }
    }
}

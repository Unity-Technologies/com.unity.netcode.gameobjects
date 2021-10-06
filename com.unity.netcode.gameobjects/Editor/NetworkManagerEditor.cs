using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace Unity.Netcode.Editor
{
    [CustomEditor(typeof(NetworkManager), true)]
    [CanEditMultipleObjects]
    public class NetworkManagerEditor : UnityEditor.Editor
    {
        internal const string InstallMultiplayerToolsTipDismissedPlayerPrefKey = "Netcode_Tip_InstallMPTools_Dismissed";
        private static GUIStyle s_CenteredWordWrappedLabelStyle;
        private static GUIStyle s_HelpBoxStyle;

        // Properties
        private SerializedProperty m_DontDestroyOnLoadProperty;
        private SerializedProperty m_RunInBackgroundProperty;
        private SerializedProperty m_LogLevelProperty;

        // NetworkConfig
        private SerializedProperty m_NetworkConfigProperty;

        // NetworkConfig fields
        private SerializedProperty m_PlayerPrefabProperty;
        private SerializedProperty m_ProtocolVersionProperty;
        private SerializedProperty m_NetworkTransportProperty;
        private SerializedProperty m_TickRateProperty;
        private SerializedProperty m_MaxObjectUpdatesPerTickProperty;
        private SerializedProperty m_ClientConnectionBufferTimeoutProperty;
        private SerializedProperty m_ConnectionApprovalProperty;
        private SerializedProperty m_EnsureNetworkVariableLengthSafetyProperty;
        private SerializedProperty m_ForceSamePrefabsProperty;
        private SerializedProperty m_EnableSceneManagementProperty;
        private SerializedProperty m_RecycleNetworkIdsProperty;
        private SerializedProperty m_NetworkIdRecycleDelayProperty;
        private SerializedProperty m_RpcHashSizeProperty;
        private SerializedProperty m_LoadSceneTimeOutProperty;

        private ReorderableList m_NetworkPrefabsList;

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
                    if (type.IsSubclassOf(typeof(NetworkTransport)) && type.GetCustomAttributes(typeof(DontShowInTransportDropdownAttribute), true).Length == 0)
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

        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            m_Initialized = true;
            m_NetworkManager = (NetworkManager)target;

            // Base properties
            m_DontDestroyOnLoadProperty = serializedObject.FindProperty(nameof(NetworkManager.DontDestroy));
            m_RunInBackgroundProperty = serializedObject.FindProperty(nameof(NetworkManager.RunInBackground));
            m_LogLevelProperty = serializedObject.FindProperty(nameof(NetworkManager.LogLevel));
            m_NetworkConfigProperty = serializedObject.FindProperty(nameof(NetworkManager.NetworkConfig));

            // NetworkConfig properties
            m_PlayerPrefabProperty = m_NetworkConfigProperty.FindPropertyRelative(nameof(NetworkConfig.PlayerPrefab));
            m_ProtocolVersionProperty = m_NetworkConfigProperty.FindPropertyRelative("ProtocolVersion");
            m_NetworkTransportProperty = m_NetworkConfigProperty.FindPropertyRelative("NetworkTransport");
            m_TickRateProperty = m_NetworkConfigProperty.FindPropertyRelative("TickRate");
            m_ClientConnectionBufferTimeoutProperty = m_NetworkConfigProperty.FindPropertyRelative("ClientConnectionBufferTimeout");
            m_ConnectionApprovalProperty = m_NetworkConfigProperty.FindPropertyRelative("ConnectionApproval");
            m_EnsureNetworkVariableLengthSafetyProperty = m_NetworkConfigProperty.FindPropertyRelative("EnsureNetworkVariableLengthSafety");
            m_ForceSamePrefabsProperty = m_NetworkConfigProperty.FindPropertyRelative("ForceSamePrefabs");
            m_EnableSceneManagementProperty = m_NetworkConfigProperty.FindPropertyRelative("EnableSceneManagement");
            m_RecycleNetworkIdsProperty = m_NetworkConfigProperty.FindPropertyRelative("RecycleNetworkIds");
            m_NetworkIdRecycleDelayProperty = m_NetworkConfigProperty.FindPropertyRelative("NetworkIdRecycleDelay");
            m_RpcHashSizeProperty = m_NetworkConfigProperty.FindPropertyRelative("RpcHashSize");
            m_LoadSceneTimeOutProperty = m_NetworkConfigProperty.FindPropertyRelative("LoadSceneTimeOut");


            ReloadTransports();
        }

        private void CheckNullProperties()
        {
            // Base properties
            m_DontDestroyOnLoadProperty = serializedObject.FindProperty(nameof(NetworkManager.DontDestroy));
            m_RunInBackgroundProperty = serializedObject.FindProperty(nameof(NetworkManager.RunInBackground));
            m_LogLevelProperty = serializedObject.FindProperty(nameof(NetworkManager.LogLevel));
            m_NetworkConfigProperty = serializedObject.FindProperty(nameof(NetworkManager.NetworkConfig));

            // NetworkConfig properties
            m_PlayerPrefabProperty = m_NetworkConfigProperty.FindPropertyRelative(nameof(NetworkConfig.PlayerPrefab));
            m_ProtocolVersionProperty = m_NetworkConfigProperty.FindPropertyRelative("ProtocolVersion");
            m_NetworkTransportProperty = m_NetworkConfigProperty.FindPropertyRelative("NetworkTransport");
            m_TickRateProperty = m_NetworkConfigProperty.FindPropertyRelative("TickRate");
            m_ClientConnectionBufferTimeoutProperty = m_NetworkConfigProperty.FindPropertyRelative("ClientConnectionBufferTimeout");
            m_ConnectionApprovalProperty = m_NetworkConfigProperty.FindPropertyRelative("ConnectionApproval");
            m_EnsureNetworkVariableLengthSafetyProperty = m_NetworkConfigProperty.FindPropertyRelative("EnsureNetworkVariableLengthSafety");
            m_ForceSamePrefabsProperty = m_NetworkConfigProperty.FindPropertyRelative("ForceSamePrefabs");
            m_EnableSceneManagementProperty = m_NetworkConfigProperty.FindPropertyRelative("EnableSceneManagement");
            m_RecycleNetworkIdsProperty = m_NetworkConfigProperty.FindPropertyRelative("RecycleNetworkIds");
            m_NetworkIdRecycleDelayProperty = m_NetworkConfigProperty.FindPropertyRelative("NetworkIdRecycleDelay");
            m_RpcHashSizeProperty = m_NetworkConfigProperty.FindPropertyRelative("RpcHashSize");
            m_LoadSceneTimeOutProperty = m_NetworkConfigProperty.FindPropertyRelative("LoadSceneTimeOut");
        }

        private void OnEnable()
        {
            m_NetworkPrefabsList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(NetworkManager.NetworkConfig)).FindPropertyRelative(nameof(NetworkConfig.NetworkPrefabs)), true, true, true, true);
            m_NetworkPrefabsList.elementHeightCallback = index =>
            {
                var networkPrefab = m_NetworkPrefabsList.serializedProperty.GetArrayElementAtIndex(index);
                var networkOverrideProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.Override));
                var networkOverrideInt = networkOverrideProp.enumValueIndex;

                return 8 + (networkOverrideInt == 0 ? EditorGUIUtility.singleLineHeight : (EditorGUIUtility.singleLineHeight * 2) + 5);
            };
            m_NetworkPrefabsList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.y += 5;

                var networkPrefab = m_NetworkPrefabsList.serializedProperty.GetArrayElementAtIndex(index);
                var networkPrefabProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.Prefab));
                var networkSourceHashProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.SourceHashToOverride));
                var networkSourcePrefabProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.SourcePrefabToOverride));
                var networkTargetPrefabProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.OverridingTargetPrefab));
                var networkOverrideProp = networkPrefab.FindPropertyRelative(nameof(NetworkPrefab.Override));
                var networkOverrideInt = networkOverrideProp.enumValueIndex;
                var networkOverrideEnum = (NetworkPrefabOverride)networkOverrideInt;
                EditorGUI.LabelField(new Rect(rect.x + rect.width - 70, rect.y, 60, EditorGUIUtility.singleLineHeight), "Override");
                if (networkOverrideEnum == NetworkPrefabOverride.None)
                {
                    if (EditorGUI.Toggle(new Rect(rect.x + rect.width - 15, rect.y, 10, EditorGUIUtility.singleLineHeight), false))
                    {
                        networkOverrideProp.enumValueIndex = (int)NetworkPrefabOverride.Prefab;
                    }
                }
                else
                {
                    if (!EditorGUI.Toggle(new Rect(rect.x + rect.width - 15, rect.y, 10, EditorGUIUtility.singleLineHeight), true))
                    {
                        networkOverrideProp.enumValueIndex = 0;
                        networkOverrideEnum = NetworkPrefabOverride.None;
                    }
                }

                if (networkOverrideEnum == NetworkPrefabOverride.None)
                {
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 80, EditorGUIUtility.singleLineHeight), networkPrefabProp, GUIContent.none);
                }
                else
                {
                    networkOverrideProp.enumValueIndex = GUI.Toolbar(new Rect(rect.x, rect.y, 100, EditorGUIUtility.singleLineHeight), networkOverrideInt - 1, new[] { "Prefab", "Hash" }) + 1;

                    if (networkOverrideEnum == NetworkPrefabOverride.Prefab)
                    {
                        EditorGUI.PropertyField(new Rect(rect.x + 110, rect.y, rect.width - 190, EditorGUIUtility.singleLineHeight), networkSourcePrefabProp, GUIContent.none);
                    }
                    else
                    {
                        EditorGUI.PropertyField(new Rect(rect.x + 110, rect.y, rect.width - 190, EditorGUIUtility.singleLineHeight), networkSourceHashProp, GUIContent.none);
                    }

                    rect.y += EditorGUIUtility.singleLineHeight + 5;

                    EditorGUI.LabelField(new Rect(rect.x, rect.y, 100, EditorGUIUtility.singleLineHeight), "Overriding Prefab");
                    EditorGUI.PropertyField(new Rect(rect.x + 110, rect.y, rect.width - 110, EditorGUIUtility.singleLineHeight), networkTargetPrefabProp, GUIContent.none);
                }
            };
            m_NetworkPrefabsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "NetworkPrefabs");
        }

        public override void OnInspectorGUI()
        {
            Initialize();
            CheckNullProperties();

#if !MULTIPLAYER_TOOLS
            DrawInstallMultiplayerToolsTip();
#endif

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

                EditorGUILayout.PropertyField(m_PlayerPrefabProperty);
                EditorGUILayout.Space();

                m_NetworkPrefabsList.DoLayoutList();
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_ProtocolVersionProperty);

                EditorGUILayout.PropertyField(m_NetworkTransportProperty);

                if (m_NetworkTransportProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("You have no transport selected. A transport is required for netcode to work. Which one do you want?", MessageType.Warning);

                    int selection = EditorGUILayout.Popup(0, m_TransportNames);

                    if (selection > 0)
                    {
                        ReloadTransports();

                        var transportComponent = m_NetworkManager.gameObject.GetComponent(m_TransportTypes[selection - 1]);

                        if (transportComponent == null)
                        {
                            transportComponent = m_NetworkManager.gameObject.AddComponent(m_TransportTypes[selection - 1]);
                        }

                        m_NetworkTransportProperty.objectReferenceValue = transportComponent;

                        Repaint();
                    }
                }

                EditorGUILayout.PropertyField(m_TickRateProperty);

                EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(m_EnsureNetworkVariableLengthSafetyProperty);

                EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_ConnectionApprovalProperty);

                using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.ConnectionApproval))
                {
                    EditorGUILayout.PropertyField(m_ClientConnectionBufferTimeoutProperty);
                }

                EditorGUILayout.LabelField("Spawning", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_ForceSamePrefabsProperty);


                EditorGUILayout.PropertyField(m_RecycleNetworkIdsProperty);

                using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.RecycleNetworkIds))
                {
                    EditorGUILayout.PropertyField(m_NetworkIdRecycleDelayProperty);
                }

                EditorGUILayout.LabelField("Bandwidth", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_RpcHashSizeProperty);

                EditorGUILayout.LabelField("Scene Management", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_EnableSceneManagementProperty);

                using (new EditorGUI.DisabledScope(!m_NetworkManager.NetworkConfig.EnableSceneManagement))
                {
                    EditorGUILayout.PropertyField(m_LoadSceneTimeOutProperty);
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

                if (m_NetworkManager.IsHost)
                {
                    instanceType = "Host";
                }
                else if (m_NetworkManager.IsServer)
                {
                    instanceType = "Server";
                }
                else if (m_NetworkManager.IsClient)
                {
                    instanceType = "Client";
                }

                EditorGUILayout.HelpBox("You cannot edit the NetworkConfig when a " + instanceType + " is running.", MessageType.Info);

                if (GUILayout.Button(new GUIContent("Stop " + instanceType, "Stops the " + instanceType + " instance.")))
                {
                    m_NetworkManager.Shutdown();
                }
            }
        }

        private static void DrawInstallMultiplayerToolsTip()
        {
            const string getToolsText = "Access additional tools for multiplayer development by installing the Multiplayer Tools package in the Package Manager.";
            const string openDocsButtonText = "Open Docs";
            const string dismissButtonText = "Dismiss";
            const string targetUrl = "https://docs-multiplayer.unity3d.com/docs/tutorials/goldenpath_series/goldenpath_foundation_module";
            const string infoIconName = "console.infoicon";

            if (PlayerPrefs.GetInt(InstallMultiplayerToolsTipDismissedPlayerPrefKey, 0) != 0)
            {
                return;
            }

            if (s_CenteredWordWrappedLabelStyle == null)
            {
                s_CenteredWordWrappedLabelStyle = new GUIStyle(GUI.skin.label);
                s_CenteredWordWrappedLabelStyle.wordWrap = true;
                s_CenteredWordWrappedLabelStyle.alignment = TextAnchor.MiddleLeft;
            }

            if (s_HelpBoxStyle == null)
            {
                s_HelpBoxStyle = new GUIStyle(EditorStyles.helpBox);
                s_HelpBoxStyle.padding = new RectOffset(10, 10, 10, 10);
            }

            var openDocsButtonStyle = GUI.skin.button;
            var dismissButtonStyle = EditorStyles.linkLabel;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(s_HelpBoxStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false), GUILayout.MaxWidth(800));
            {
                GUILayout.Label(new GUIContent(EditorGUIUtility.IconContent(infoIconName)), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));
                GUILayout.Space(4);
                GUILayout.Label(getToolsText, s_CenteredWordWrappedLabelStyle, GUILayout.ExpandHeight(true));

                GUILayout.Space(4);

                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(openDocsButtonText, openDocsButtonStyle, GUILayout.Width(90), GUILayout.Height(30)))
                {
                    Application.OpenURL(targetUrl);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                GUILayout.Space(4);

                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(dismissButtonText, dismissButtonStyle, GUILayout.ExpandWidth(false)))
                {
                    PlayerPrefs.SetInt(InstallMultiplayerToolsTipDismissedPlayerPrefKey, 1);
                }
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }
    }
}

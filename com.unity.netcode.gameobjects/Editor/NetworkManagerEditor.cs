#if UNITY_2022_3_OR_NEWER && (RELAY_SDK_INSTALLED && !UNITY_WEBGL ) || (RELAY_SDK_INSTALLED && UNITY_WEBGL && UTP_TRANSPORT_2_0_ABOVE)
#define RELAY_INTEGRATION_AVAILABLE
#endif
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode.Editor.Configuration;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// This <see cref="CustomEditor"/> handles the translation between the <see cref="NetworkConfig"/> and
    /// the <see cref="NetworkManager"/> properties.
    /// </summary>
    [CustomEditor(typeof(NetworkManager), true)]
    [CanEditMultipleObjects]
    public class NetworkManagerEditor : UnityEditor.Editor
    {
        private static GUIStyle s_CenteredWordWrappedLabelStyle;
        private static GUIStyle s_HelpBoxStyle;

        // Properties
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
        private SerializedProperty m_PrefabsList;

        private NetworkManager m_NetworkManager;
        private bool m_Initialized;

        private readonly List<Type> m_TransportTypes = new List<Type>();
        private string[] m_TransportNames = { "Select transport..." };

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            Initialize();
            CheckNullProperties();

#if !MULTIPLAYER_TOOLS
            DrawInstallMultiplayerToolsTip();
#endif

            if (m_NetworkManager.IsServer || m_NetworkManager.IsClient)
            {
                DrawDisconnectButton();
            }
            else
            {
                DrawAllPropertyFields();
                ShowStartConnectionButtons();
            }
        }

        private void ReloadTransports()
        {
            m_TransportTypes.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    if (type.IsSubclassOf(typeof(NetworkTransport)) && !type.IsSubclassOf(typeof(TestingNetworkTransport)) && type != typeof(TestingNetworkTransport))
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
            m_PrefabsList = m_NetworkConfigProperty
                .FindPropertyRelative(nameof(NetworkConfig.Prefabs))
                .FindPropertyRelative(nameof(NetworkPrefabs.NetworkPrefabsLists));

            ReloadTransports();
        }

        private void CheckNullProperties()
        {
            // Base properties
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
            m_PrefabsList = m_NetworkConfigProperty
                .FindPropertyRelative(nameof(NetworkConfig.Prefabs))
                .FindPropertyRelative(nameof(NetworkPrefabs.NetworkPrefabsLists));
        }

        private void DrawAllPropertyFields()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_RunInBackgroundProperty);
            EditorGUILayout.PropertyField(m_LogLevelProperty);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_PlayerPrefabProperty);
            EditorGUILayout.Space();

            DrawPrefabListField();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ProtocolVersionProperty);

            DrawTransportField();

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
        }

        private void DrawTransportField()
        {
#if RELAY_INTEGRATION_AVAILABLE
            var useRelay = EditorPrefs.GetBool(m_UseEasyRelayIntegrationKey, false);
#else
            var useRelay = false;
#endif

            if (useRelay)
            {
                EditorGUILayout.HelpBox("Test connection with relay is enabled, so the default Unity Transport will be used", MessageType.Info);
                GUI.enabled = false;
                EditorGUILayout.PropertyField(m_NetworkTransportProperty);
                GUI.enabled = true;
                return;
            }

            EditorGUILayout.PropertyField(m_NetworkTransportProperty);

            if (m_NetworkTransportProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("You have no transport selected. A transport is required for netcode to work. Which one do you want?", MessageType.Warning);

                int selection = EditorGUILayout.Popup(0, m_TransportNames);

                if (selection > 0)
                {
                    ReloadTransports();

                    var transportComponent = m_NetworkManager.gameObject.GetComponent(m_TransportTypes[selection - 1]) ?? m_NetworkManager.gameObject.AddComponent(m_TransportTypes[selection - 1]);
                    m_NetworkTransportProperty.objectReferenceValue = transportComponent;

                    Repaint();
                }
            }
        }

#if RELAY_INTEGRATION_AVAILABLE
        private readonly string m_UseEasyRelayIntegrationKey = "NetworkManagerUI_UseRelay_" + Application.dataPath.GetHashCode();
        private string m_JoinCode = "";
        private string m_StartConnectionError = null;
        private string m_Region = "";

        // wait for next frame so that ImGui finishes the current frame
        private static void RunNextFrame(Action action) => EditorApplication.delayCall += () => action();
#endif

        private void ShowStartConnectionButtons()
        {
            EditorGUILayout.LabelField("Start Connection", EditorStyles.boldLabel);

#if RELAY_INTEGRATION_AVAILABLE
            // use editor prefs to persist the setting when entering / leaving play mode / exiting Unity
            var useRelay = EditorPrefs.GetBool(m_UseEasyRelayIntegrationKey, false);
            GUILayout.BeginHorizontal();
            useRelay = GUILayout.Toggle(useRelay, "Try Relay in the Editor");

            var icon = EditorGUIUtility.IconContent("_Help");
            icon.tooltip = "This will help you test relay in the Editor. Click here to know how to integrate Relay in your build";
            if (GUILayout.Button(icon, GUIStyle.none, GUILayout.Width(20)))
            {
                Application.OpenURL("https://docs-multiplayer.unity3d.com/netcode/current/relay/");
            }
            GUILayout.EndHorizontal();

            EditorPrefs.SetBool(m_UseEasyRelayIntegrationKey, useRelay);
            if (useRelay && !Application.isPlaying && !CloudProjectSettings.projectBound)
            {
                EditorGUILayout.HelpBox("To use relay, you need to setup your project in the Project Settings in the Services section.", MessageType.Warning);
                if (GUILayout.Button("Open Project settings"))
                {
                    SettingsService.OpenProjectSettings("Project/Services");
                }
            }
#else
            var useRelay = false;
#endif

            string buttonDisabledReasonSuffix = "";

            if (!EditorApplication.isPlaying)
            {
                buttonDisabledReasonSuffix = ". This can only be done in play mode";
                GUI.enabled = false;
            }

            if (useRelay)
            {
                ShowStartConnectionButtons_Relay(buttonDisabledReasonSuffix);
            }
            else
            {
                ShowStartConnectionButtons_Standard(buttonDisabledReasonSuffix);
            }

            if (!EditorApplication.isPlaying)
            {
                GUI.enabled = true;
            }
        }

        private void ShowStartConnectionButtons_Relay(string buttonDisabledReasonSuffix)
        {
#if RELAY_INTEGRATION_AVAILABLE

            void AddStartServerOrHostButton(bool isServer)
            {
                var type = isServer ? "Server" : "Host";
                if (GUILayout.Button(new GUIContent($"Start {type}", $"Starts a {type} instance with Relay{buttonDisabledReasonSuffix}")))
                {
                    m_StartConnectionError = null;
                    RunNextFrame(async () =>
                    {
                        try
                        {
                            var (joinCode, allocation) = isServer ? await m_NetworkManager.StartServerWithRelay() : await m_NetworkManager.StartHostWithRelay();
                            m_JoinCode = joinCode;
                            m_Region = allocation.Region;
                            Repaint();
                        }
                        catch (Exception e)
                        {
                            m_StartConnectionError = e.Message;
                            throw;
                        }
                    });
                }
            }

            AddStartServerOrHostButton(isServer: true);
            AddStartServerOrHostButton(isServer: false);

            GUILayout.Space(8f);
            m_JoinCode = EditorGUILayout.TextField("Relay Join Code", m_JoinCode);
            if (GUILayout.Button(new GUIContent("Start Client", "Starts a client instance with Relay" + buttonDisabledReasonSuffix)))
            {
                m_StartConnectionError = null;
                RunNextFrame(async () =>
                {
                    if (string.IsNullOrEmpty(m_JoinCode))
                    {
                        m_StartConnectionError = "Please provide a join code!";
                        return;
                    }

                    try
                    {
                        var allocation = await m_NetworkManager.StartClientWithRelay(m_JoinCode);
                        m_Region = allocation.Region;
                        Repaint();
                    }
                    catch (Exception e)
                    {
                        m_StartConnectionError = e.Message;
                        throw;
                    }
                });
            }

            if (Application.isPlaying && !string.IsNullOrEmpty(m_StartConnectionError))
            {
                EditorGUILayout.HelpBox(m_StartConnectionError, MessageType.Error);
            }
#endif
        }

        private void ShowStartConnectionButtons_Standard(string buttonDisabledReasonSuffix)
        {
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
        }

        private void DrawDisconnectButton()
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

            EditorGUILayout.HelpBox($"You cannot edit the NetworkConfig when a {instanceType} is running.", MessageType.Info);

#if RELAY_INTEGRATION_AVAILABLE
            if (!string.IsNullOrEmpty(m_JoinCode) && !string.IsNullOrEmpty(m_Region))
            {
                var style = new GUIStyle(EditorStyles.helpBox)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter,
                };

                GUILayout.BeginHorizontal(style, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false), GUILayout.MaxWidth(800));
                GUILayout.Label(new GUIContent(EditorGUIUtility.IconContent(k_InfoIconName)), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));
                GUILayout.Space(25f);
                GUILayout.BeginVertical();
                GUILayout.Space(4f);
                GUILayout.Label($"Connected via relay ({m_Region}).\nJoin code: {m_JoinCode}", EditorStyles.miniLabel, GUILayout.ExpandHeight(true));

                if (GUILayout.Button("Copy code", GUILayout.ExpandHeight(true)))
                {
                    GUIUtility.systemCopyBuffer = m_JoinCode;
                }

                GUILayout.Space(4f);
                GUILayout.EndVertical();
                GUILayout.Space(2f);
                GUILayout.EndHorizontal();
            }
#endif

            if (GUILayout.Button(new GUIContent($"Stop {instanceType}", $"Stops the {instanceType} instance.")))
            {
#if RELAY_INTEGRATION_AVAILABLE
                m_JoinCode = "";
#endif
                m_NetworkManager.Shutdown();
            }
        }

        private const string k_InfoIconName = "console.infoicon";
        private static void DrawInstallMultiplayerToolsTip()
        {
            const string getToolsText = "Access additional tools for multiplayer development by installing the Multiplayer Tools package in the Package Manager.";
            const string openDocsButtonText = "Open Docs";
            const string dismissButtonText = "Dismiss";
            const string targetUrl = "https://docs-multiplayer.unity3d.com/tools/current/install-tools";


            if (NetcodeForGameObjectsEditorSettings.GetNetcodeInstallMultiplayerToolTips() != 0)
            {
                return;
            }

            if (s_CenteredWordWrappedLabelStyle == null)
            {
                s_CenteredWordWrappedLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (s_HelpBoxStyle == null)
            {
                s_HelpBoxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }

            var openDocsButtonStyle = GUI.skin.button;
            var dismissButtonStyle = EditorStyles.linkLabel;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(s_HelpBoxStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false), GUILayout.MaxWidth(800));
            {
                GUILayout.Label(new GUIContent(EditorGUIUtility.IconContent(k_InfoIconName)), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));
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
                    NetcodeForGameObjectsEditorSettings.SetNetcodeInstallMultiplayerToolTips(1);
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

        private void DrawPrefabListField()
        {
            if (!m_NetworkManager.NetworkConfig.HasOldPrefabList())
            {
                if (m_NetworkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Count == 0)
                {
                    EditorGUILayout.HelpBox("You have no prefab list selected. You will have to add your prefabs manually at runtime for netcode to work.", MessageType.Warning);
                }

                EditorGUILayout.PropertyField(m_PrefabsList);
                return;
            }

            // Old format of prefab list
            EditorGUILayout.HelpBox("Network Prefabs serialized in old format. Migrate to new format to edit the list.", MessageType.Info);
            if (GUILayout.Button(new GUIContent("Migrate Prefab List", "Converts the old format Network Prefab list to a new Scriptable Object")))
            {
                // Default directory
                var directory = "Assets/";
                var assetPath = AssetDatabase.GetAssetPath(m_NetworkManager);
                if (assetPath == "")
                {
                    assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m_NetworkManager);
                }

                if (assetPath != "")
                {
                    directory = Path.GetDirectoryName(assetPath);
                }
                else
                {

#if UNITY_2021_1_OR_NEWER
                    var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(m_NetworkManager.gameObject);
#else
                    var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(m_NetworkManager.gameObject);
#endif
                    if (prefabStage != null)
                    {
                        var prefabPath = prefabStage.assetPath;
                        if (!string.IsNullOrEmpty(prefabPath))
                        {
                            directory = Path.GetDirectoryName(prefabPath);
                        }
                    }

                    if (m_NetworkManager.gameObject.scene != null)
                    {
                        var scenePath = m_NetworkManager.gameObject.scene.path;
                        if (!string.IsNullOrEmpty(scenePath))
                        {
                            directory = Path.GetDirectoryName(scenePath);
                        }
                    }
                }

                var networkPrefabs = m_NetworkManager.NetworkConfig.MigrateOldNetworkPrefabsToNetworkPrefabsList();
                string path = Path.Combine(directory, $"NetworkPrefabs-{m_NetworkManager.GetInstanceID()}.asset");
                Debug.Log("Saving migrated Network Prefabs List to " + path);
                AssetDatabase.CreateAsset(networkPrefabs, path);
                EditorUtility.SetDirty(m_NetworkManager);
            }
        }
    }
}

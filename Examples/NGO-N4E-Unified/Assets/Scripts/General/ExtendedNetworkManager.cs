using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.NetCode;
#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools.NetStatsMonitor;
#endif
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;
using SessionState = Unity.Services.Multiplayer.SessionState;
using UnityEngine.SceneManagement;

#region ExtendedNetworkManagerEditor
#if UNITY_EDITOR
using Unity.Netcode.GameObjects.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using WebSocketSharp;
/// <summary>
/// The custom editor for the <see cref="NetworkManagerBootstrapper"/> component.
/// </summary>
[CustomEditor(typeof(ExtendedNetworkManager), true)]
[CanEditMultipleObjects]
public class ExtendedNetworkManagerEditor : NetworkManagerEditor
{
    private SerializedProperty m_TargetFrameRate;
    private SerializedProperty m_EnableVSync;
    private SerializedProperty m_UseDAHost;
    private SerializedProperty m_AuthenticateWithServices;
    private SerializedProperty m_GravityMultiplier;

    public override void OnEnable()
    {
        m_TargetFrameRate = serializedObject.FindProperty(nameof(ExtendedNetworkManager.TargetFrameRate));
        m_EnableVSync = serializedObject.FindProperty(nameof(ExtendedNetworkManager.EnableVSync));
        m_UseDAHost = serializedObject.FindProperty(nameof(ExtendedNetworkManager.UseDAHost));
        m_AuthenticateWithServices = serializedObject.FindProperty(nameof(ExtendedNetworkManager.AuthenticateWithServices));
        m_GravityMultiplier = serializedObject.FindProperty(nameof(ExtendedNetworkManager.GravityMultiplier));
        base.OnEnable();
    }

    private void DisplayNetworkManagerBootstrapperProperties()
    {
        var extendedNetworkManager = target as ExtendedNetworkManager;
        EditorGUILayout.PropertyField(m_TargetFrameRate);
        EditorGUILayout.PropertyField(m_EnableVSync);
        var useDAHost = EditorGUILayout.PropertyField(m_UseDAHost);
        if (extendedNetworkManager.UseDAHost)
        {
            extendedNetworkManager.AuthenticateWithServices = false;
        }
        GUI.enabled = !extendedNetworkManager.UseDAHost;
        EditorGUILayout.PropertyField(m_AuthenticateWithServices);
        GUI.enabled = true;
        EditorGUILayout.PropertyField(m_GravityMultiplier);
    }

    public override void OnInspectorGUI()
    {
        var extendedNetworkManager = target as ExtendedNetworkManager;
        void SetExpanded(bool expanded) { extendedNetworkManager.NetworkManagerBootstrapperExpanded = expanded; }
        ;
        DrawFoldOutGroup<ExtendedNetworkManager>(extendedNetworkManager.GetType(), DisplayNetworkManagerBootstrapperProperties, extendedNetworkManager.NetworkManagerBootstrapperExpanded, SetExpanded);
        base.OnInspectorGUI();
    }
}
#endif
#endregion

/// <summary>
/// An extended NetworkManager to handle the bootstrap loading process specific to a client-server
/// topology where one might want to have local server-side scenes, local client-side scenes, and shared (synchronized) scenes.
/// <see cref="SceneBootstrapLoader"/>
/// </summary>
public class ExtendedNetworkManager : NetworkManager
{
    #region Validation
#if UNITY_EDITOR

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleScene")
        {
            UpdateServicesInfo();
        }
        else
        {
            EditorSceneManager.sceneOpened -= SceneOpened;
            EditorSceneManager.sceneOpened += SceneOpened;
        }
    }

    private static void SceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.name == "SampleScene")
        {
            UpdateServicesInfo();
        }
    }

    private static void UpdateServicesInfo()
    {
        var extendedNetworkManager = GameObject.Find("ExtendedNetworkManager")?.GetComponent<ExtendedNetworkManager>();
        if (extendedNetworkManager == null)
        {
            return;
        }
        extendedNetworkManager.CheckServiceStatus();
    }

    // Inspector view expand/collapse settings for this derived child class
    [HideInInspector]
    public bool NetworkManagerBootstrapperExpanded;
    protected override void OnValidateComponent()
    {
        m_OriginalVSyncCount = QualitySettings.vSyncCount;
        CheckServiceStatus();

        m_UsingHybridSpawning = false;
        if (NetworkConfig.Prefabs.NetworkPrefabsLists.Count > 0)
        {
            foreach (var prefabList in NetworkConfig.Prefabs.NetworkPrefabsLists)
            {
                foreach (var prefabEntry in prefabList.PrefabList)
                {
                    if (prefabEntry.Prefab != null & prefabEntry.Prefab.GetComponent<GhostAdapter>())
                    {
                        m_UsingHybridSpawning = true;
                        break;
                    }
                }
            }
        }

        base.OnValidateComponent();
    }

    private void CheckServiceStatus()
    {
        m_ServicesRegistered = CloudProjectSettings.organizationName != string.Empty && CloudProjectSettings.organizationId != string.Empty;
    }
#endif
    #endregion

    #region Properties
    public static ExtendedNetworkManager Instance;

    public int TargetFrameRate = 100;
    public bool EnableVSync = false;
    public bool UseDAHost = true;
    public bool AuthenticateWithServices = true;

    public SceneLoader SceneLoader => m_SceneLoader;
    private SceneLoader m_SceneLoader;

    [HideInInspector]
    [SerializeField]
    private int m_OriginalVSyncCount;

    private enum ConnectionStates
    {
        None,
        Connecting,
        Connected,
    }

    public bool IsConnectedToSession => m_ConnectionState == ConnectionStates.Connected;

    private ConnectionStates m_ConnectionState;

    [SerializeField]
    private bool m_ServicesRegistered;

    [HideInInspector]
    [SerializeField]
    private bool m_UsingHybridSpawning;

    private ISession m_CurrentSession;
    private string m_SessionName;
    private string m_ProfileName;
    private Task m_SessionTask;
    [Range(0.1f, 10.0f)]
    public float GravityMultiplier = 3.0f;
    private float m_CurrentGravityMultiplier = 0.0f;
    private Vector3 StandardGravity = new Vector3(0.0f, -9.81f, 0.0f);
    #endregion

    #region Application Exit 
    public static Action OnExiting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void OnInitialize()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += PlayModeStateChanged;
#else
        Application.wantsToQuit += Application_wantsToQuit;
#endif
    }


#if UNITY_EDITOR
    private static void PlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            OnExiting?.Invoke();
        }
    }
#else
    private static bool Application_wantsToQuit()
    {
        OnExiting?.Invoke();
        return true;
    }
#endif
    #endregion

    #region NetStats Monitor

#if MULTIPLAYER_TOOLS
    [Tooltip("Assign an in-scene placed net stats monitor prefab here for it to be available to other components.")]
    public RuntimeNetStatsMonitor NetStatsMonitor;
#endif
    public void ToggleNetStatsMonitor(bool disable = false)
    {
#if MULTIPLAYER_TOOLS
        if (NetStatsMonitor)
        {
            if (!NetworkConfig.NetworkMessageMetrics)
            {
                var checkForBuiltIn = false;
                var defaultMetrics = Enum.GetNames(typeof(Unity.Multiplayer.Tools.MetricTypes.DirectedMetricType));
                foreach (var elementConfig in NetStatsMonitor.Configuration.DisplayElements)
                {
                    foreach (var stats in elementConfig.Stats)
                    {
                        if (defaultMetrics.Contains(stats.ToString()))
                        {
                            checkForBuiltIn = true;
                            break;
                        }
                    }
                    if (checkForBuiltIn)
                    {
                        break;
                    }
                }

                // Log a warning if the messaging metrics is disabled
                if (checkForBuiltIn)
                {
                    Debug.LogWarning($"{nameof(NetworkManager)}'s {nameof(NetworkConfig.NetworkMessageMetrics)} property is not enabled. The default Built-In Metrics use {nameof(NetworkConfig.NetworkMessageMetrics)} and {nameof(NetStatsMonitor)} not be populates with values!");
                }
            }
            if (disable)
            {
                NetStatsMonitor.Visible = false;
            }
            else
            {
                NetStatsMonitor.Visible = !NetStatsMonitor.Visible;
            }

        }
#else
        Debug.LogWarning($"The multiplayer tools package is not installed!");
#endif
    }
    #endregion

    #region Tracking Pool System Loading

    public ProgressFill ProgressBar;
    public bool ByPassPoolLoading = false;


    private Dictionary<ObjectPoolSystem, float> m_PoolSystemsLoading = new Dictionary<ObjectPoolSystem, float>();

    /// <summary>
    /// Used by ObjectPoolSystem to visually track the progress of instantiating its object pool
    /// </summary>
    public void TrackPoolSystemLoading(ObjectPoolSystem poolSystem, float progress, bool isLoading = true)
    {
        if (isLoading)
        {
            if (!m_PoolSystemsLoading.ContainsKey(poolSystem))
            {
                m_PoolSystemsLoading.Add(poolSystem, progress);
            }
            else
            {
                m_PoolSystemsLoading[poolSystem] = progress;
            }
        }
        else
        {
            m_PoolSystemsLoading.Remove(poolSystem);
        }
    }

    private void UpdateProgress()
    {
        if (ByPassPoolLoading)
        {
            return;
        }

        var totalProgress = 0.0f;
        foreach (var poolSystem in m_PoolSystemsLoading)
        {
            totalProgress += poolSystem.Value;
        }
        totalProgress = totalProgress / m_PoolSystemsLoading.Count;
        ProgressBar.UpdateProgress(totalProgress);
        if (totalProgress >= 1.0f)
        {
            m_PoolSystemsLoading.Clear();
        }
    }
    #endregion

    #region Initialization and Destroy
    public static string GetRandomString(int length)
    {
        var r = new System.Random();
        return new string(Enumerable.Range(0, length).Select(_ => (char)r.Next('a', 'z')).ToArray());
    }

    public void SetFrameRate(int targetFrameRate, bool enableVsync)
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = enableVsync ? m_OriginalVSyncCount : 0;
    }

    private async void Start()
    {
#if UNITY_EDITOR

        if (!EditorApplication.isPlaying)
        {
            CheckServiceStatus();
        }
#endif
        Screen.SetResolution((int)(Screen.currentResolution.width * 0.40f), (int)(Screen.currentResolution.height * 0.40f), FullScreenMode.Windowed);
        SetFrameRate(TargetFrameRate, EnableVSync);
        SetSingleton();
        m_SceneLoader = GetComponent<SceneLoader>();

        OnClientConnectedCallback += OnClientConnected;
        OnClientDisconnectCallback += OnClientDisconnect;
        OnConnectionEvent += OnClientConnectionEvent;
        try
        {
            // Check to see if the project has been registered with an organization before trying to sign in
            if (m_ServicesRegistered && AuthenticateWithServices)
            {
                if (UnityServices.Instance != null && UnityServices.Instance.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignInFailed += SignInFailed;
                    AuthenticationService.Instance.SignedIn += SignedIn;
                    if (string.IsNullOrEmpty(m_ProfileName))
                    {
                        m_ProfileName = GetRandomString(5);
                    }
                    AuthenticationService.Instance.SwitchProfile(m_ProfileName);
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"This project might need to be registered with your services account.\n{ex.Message}\n{ex.StackTrace}");
        }

        // Handle bootstrap loading the main menu into main menu
        // NOTE: A common issue is when you place the NetworkManager within the same "primary" scene to 
        // load. Using the bootstrap approach, the 1st scene loaded is the bootstrap scene that 
        // contains the NetworkManager and it is never reloaded for that application instance lifespan.
        if (m_SceneLoader)
        {
            m_SceneLoader.LoadMainMenu();
        }
    }

    private void OnDestroy()
    {
        OnClientConnectedCallback -= OnClientConnected;
        OnClientDisconnectCallback -= OnClientDisconnect;
        OnConnectionEvent -= OnClientConnectionEvent;
    }
    #endregion

    #region Session and Connection Event Handling
    public Action OnShuttingDown;

    /// <summary>
    /// Server and Clients all invoke this method
    /// </summary>
    private void OnClientConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        LogMessage($"Connection event {eventData.EventType} for Client-{eventData.ClientId}.");
        if (eventData.ClientId != LocalClientId)
        {
            return;
        }

        switch (eventData.EventType)
        {
            case ConnectionEvent.ClientConnected:
                {
                    m_ConnectionState = ConnectionStates.Connected;
                    break;
                }
            case ConnectionEvent.ClientDisconnected:
                {
                    m_ConnectionState = ConnectionStates.None;
                    break;
                }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        LogMessage($"Connected event invoked for Client-{clientId}.");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        LogMessage($"Disconnected event invoked for Client-{clientId}.");
    }

    private void SignedIn()
    {
        AuthenticationService.Instance.SignedIn -= SignedIn;
        Debug.Log($"Signed in anonymously with profile {m_ProfileName}");
    }

    private void SignInFailed(RequestFailedException error)
    {
        AuthenticationService.Instance.SignInFailed -= SignInFailed;
        Debug.LogError($"Failed to sign in {m_ProfileName} anonymously: {error}");
    }

    private void SessionStarted()
    {

        if (GravityMultiplier != m_CurrentGravityMultiplier)
        {
            m_CurrentGravityMultiplier = GravityMultiplier;
            Physics.gravity = StandardGravity * m_CurrentGravityMultiplier;
        }

        OnClientStarted -= SessionStarted;
        m_ConnectionState = IsServer && !IsHost ? ConnectionStates.Connected : ConnectionStates.Connecting;
        if (IsServer)
        {
            LogMessage($"Server started session.");
        }
        else
        {
            LogMessage($"Client connecting to session.");
        }
        if (DistributedAuthorityMode && (CMBServiceConnection || !DAHost))
        {
            m_SceneLoader.DAClientStarted();
        }
    }

    private void SessionStopped(bool isHost)
    {
        LogMessage($"NetworkManager has stopped.");
        OnClientStopped -= SessionStopped;
        m_ConnectionState = ConnectionStates.None;
    }

    private async Task<ISession> ConnectThroughLiveService()
    {
        try
        {
            var options = new SessionOptions()
            {
                Name = m_SessionName,
                MaxPlayers = 32
            }.WithDistributedAuthorityNetwork();

            m_CurrentSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(m_SessionName, options);
            return m_CurrentSession;
        }
        catch (Exception e)
        {
            LogMessage($"{e.Message}");
            Debug.LogException(e);
        }
        return null;
    }
    #endregion

    #region GUI Menu
    public void StartOrConnectToDistributedAuthoritySession()
    {
        m_SessionTask = ConnectThroughLiveService();
        m_ConnectionState = ConnectionStates.Connecting;
        LogMessage($"Connecting to session {m_SessionName}...");
    }

    private void OnUpdateGUIDisconnected()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 800));

        if (NetworkConfig.NetworkTopology == NetworkTopologyTypes.DistributedAuthority)
        {
            if (!m_ServicesRegistered && AuthenticateWithServices)
            {
                GUILayout.Label("Project-Settings:Services-General-Settings is not configured.");
                GUILayout.Label("Distributed authority requires project to be registered with your organization's services account for authentication purposes.");
            }
            else
            {
                if (UseDAHost)
                {
                    if (GUILayout.Button("Start DAHost"))
                    {
                        OnServerStopped += SessionStopped;
                        OnServerStarted += SessionStarted;
                        StartHost();
                    }
                    else
                    if (GUILayout.Button("Start DAClient"))
                    {
                        OnClientStopped += SessionStopped;
                        OnClientStarted += SessionStarted;
                        StartClient();
                    }
                }
                else
                {
                    if (AuthenticateWithServices)
                    {
                        m_SessionName = GUILayout.TextField(m_SessionName);
                        if (GUILayout.Button("Create or Connect To Session"))
                        {
                            NetworkConfig.UseCMBService = true;
                            OnClientStopped += SessionStopped;
                            OnClientStarted += SessionStarted;
                            StartOrConnectToDistributedAuthoritySession();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Connect To Local Service"))
                        {
                            NetworkConfig.UseCMBService = true;
                            OnClientStopped += SessionStopped;
                            OnClientStarted += SessionStarted;
                            StartClient();
                        }
                    }
                }
            }
        }
        else
        {
            if (m_UsingHybridSpawning)
            {
                if (GUILayout.Button("Hybrid-Host"))
                {
                    OnServerStopped += SessionStopped;
                    OnServerStarted += SessionStarted;
                    StartHost();
                }

                if (GUILayout.Button("Hybrid-Client"))
                {
                    OnClientStopped += SessionStopped;
                    OnClientStarted += SessionStarted;
                    StartClient();
                }
            }
            else
            {
                if (GUILayout.Button("Start Server"))
                {
                    OnServerStopped += SessionStopped;
                    OnServerStarted += SessionStarted;
                    StartServer();
                }
                else
                if (GUILayout.Button("Start Host"))
                {
                    OnServerStopped += SessionStopped;
                    OnServerStarted += SessionStarted;
                    StartHost();
                }
                else
                if (GUILayout.Button("Start Client"))
                {
                    OnClientStopped += SessionStopped;
                    OnClientStarted += SessionStarted;
                    StartClient();
                }
            }
        }
        GUILayout.EndArea();
    }

    public string CustomHeader;

    public Func<int, int> OnConnectedGUIUpdate;

    private int OnUpdateGUIConnected(int yAxisOffset)
    {
        #region Left side
        GUILayout.BeginArea(new Rect(10, 10, 800, 800));
        if (CMBServiceConnection)
        {
            GUILayout.Label($"Distributed Authority Session: {m_SessionName}");
            if (LocalClient.IsSessionOwner)
            {
                GUILayout.Label("[Session Owner]");
                yAxisOffset += 20;
            }
        }
        else
        {
            if (m_UsingHybridSpawning)
            {
                GUILayout.Label($"NGO & N4E Client-Server Hybrid Session");
            }
            else
            {
                GUILayout.Label($"Client-Server Session");
            }
        }
        if (!string.IsNullOrEmpty(CustomHeader))
        {
            GUILayout.Label($"{CustomHeader}");
        }

        yAxisOffset = OnConnectedGUIUpdate?.Invoke(yAxisOffset) ?? yAxisOffset;
        GUILayout.EndArea();
        #endregion

        #region Right side
        GUILayout.BeginArea(new Rect(Display.main.renderingWidth - 230, 10, 220, 300));
        var endSessionText = IsServer && !DistributedAuthorityMode ? "Shutdown" : "Disconnect";
        if (GUILayout.Button(endSessionText))
        {
            OnShuttingDown?.Invoke();
            if (m_CurrentSession != null && m_CurrentSession.State == SessionState.Connected)
            {
                m_CurrentSession.LeaveAsync();
                m_CurrentSession = null;
            }
            else
            {
                Shutdown();
            }
        }
        if (m_SceneLoader && !m_SceneLoader.SceneLoadingInProgress)
        {
            GUILayout.Label($"Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            var currentPlayerPrefab = m_SceneLoader.GetCurrentPrefabName();
            if (!string.IsNullOrEmpty(currentPlayerPrefab))
            {
                GUILayout.Label($"Current player-prefab: {currentPlayerPrefab}");
            }
            if (IsSessionAuthority())
            {
                var currentSceneEntry = m_SceneLoader.CurrentSceneEntry;
                if (currentSceneEntry != null)
                {
                    var nextPlayerPrefab = m_SceneLoader.GetNextPrefabName();
                    var nextScene = $"Load {m_SceneLoader.GetNextSceneName()}";
                    var buttontext = string.IsNullOrEmpty(nextPlayerPrefab) ? nextScene : $"{nextScene}\nusing {nextPlayerPrefab}";
                    if (GUILayout.Button(buttontext))
                    {
                        m_SceneLoader.LoadNextScene();
                    }
                }
            }
        }
        GUILayout.EndArea();
        #endregion
        return yAxisOffset;
    }

    private void OnGUI()
    {
        var yAxisOffset = 10;
        switch (m_ConnectionState)
        {
            case ConnectionStates.None:
                {
                    yAxisOffset = 80;
                    OnUpdateGUIDisconnected();
                    break;
                }
            case ConnectionStates.Connected:
                {
                    yAxisOffset = OnUpdateGUIConnected(40);
                    break;
                }
        }

        GUILayout.BeginArea(new Rect(10, yAxisOffset, 800, 800));
        if (m_MessageLogs.Count > 0)
        {
            GUILayout.Label("-----------(Log)-----------");
            // Display any messages logged to screen
            foreach (var messageLog in m_MessageLogs)
            {
                GUILayout.Label(messageLog.Message);
            }
            GUILayout.Label("---------------------------");
        }
        GUILayout.EndArea();
    }
    #endregion

    #region Update and Status Methods
    private void UpdateRuntimeMessageConsole()
    {
        if (m_MessageLogs.Count == 0)
        {
            return;
        }

        for (int i = m_MessageLogs.Count - 1; i >= 0; i--)
        {
            if (m_MessageLogs[i].ExpirationTime < Time.realtimeSinceStartup)
            {
                m_MessageLogs.RemoveAt(i);
            }
        }
    }

    public bool IsSessionAuthority()
    {
        if (!DistributedAuthorityMode)
        {
            return IsServer;
        }
        else
        {
            return LocalClientId == CurrentSessionOwner;
        }
    }

    /// <summary>
    /// Invoked when a network session is active
    /// </summary>
    private void ConnectedUpdate()
    {
        UpdateRuntimeMessageConsole();
    }

    /// <summary>
    /// Invoked when no network session is active
    /// </summary>
    private void DisconnectedUpdate()
    {
        if (m_PoolSystemsLoading.Count > 0)
        {
            UpdateProgress();
        }
    }

    private void Update()
    {
        if (IsConnectedToSession)
        {
            ConnectedUpdate();
        }
        else
        {
            DisconnectedUpdate();
        }
    }
    #endregion

    #region Message Logging

    private List<MessageLog> m_MessageLogs = new List<MessageLog>();

    private class MessageLog
    {
        public string Message { get; private set; }
        public float ExpirationTime { get; private set; }

        public MessageLog(string msg, float timeToLive)
        {
            Message = msg;
            ExpirationTime = Time.realtimeSinceStartup + timeToLive;
        }
    }

    public void LogMessage(string msg, float timeToLive = 10.0f)
    {
        msg = $"[{Time.realtimeSinceStartup}] {msg}";
        if (m_MessageLogs.Count > 0)
        {
            m_MessageLogs.Insert(0, new MessageLog(msg, timeToLive));
        }
        else
        {
            m_MessageLogs.Add(new MessageLog(msg, timeToLive));
        }
        Debug.Log(msg);
    }
    #endregion

    public ExtendedNetworkManager()
    {
        Instance = this;
    }
}

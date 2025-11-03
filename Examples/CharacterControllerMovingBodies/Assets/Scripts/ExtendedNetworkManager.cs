using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using SessionState = Unity.Services.Multiplayer.SessionState;


#region ExtendedNetworkManagerEditor
#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;

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
    private SerializedProperty m_UseRelayConnection;

    public override void OnEnable()
    {
        m_TargetFrameRate = serializedObject.FindProperty(nameof(ExtendedNetworkManager.TargetFrameRate));
        m_EnableVSync = serializedObject.FindProperty(nameof(ExtendedNetworkManager.EnableVSync));
        m_UseDAHost = serializedObject.FindProperty(nameof(ExtendedNetworkManager.UseDAHost));
        m_AuthenticateWithServices = serializedObject.FindProperty(nameof(ExtendedNetworkManager.AuthenticateWithServices));
        m_UseRelayConnection = serializedObject.FindProperty(nameof(ExtendedNetworkManager.UseRelayConnection));
        base.OnEnable();
    }

    private void DisplayExtendedNetworkManagerProperties()
    {
        EditorGUILayout.PropertyField(m_TargetFrameRate);
        EditorGUILayout.PropertyField(m_EnableVSync);
        EditorGUILayout.PropertyField(m_UseDAHost);
        EditorGUILayout.PropertyField(m_AuthenticateWithServices);
        var extendedNetworkManager = target as ExtendedNetworkManager;
        if (extendedNetworkManager.AuthenticateWithServices)
        {
            EditorGUILayout.PropertyField(m_UseRelayConnection);
        }
        else
        {

            extendedNetworkManager.UseRelayConnection = false;
        }
    }

    public override void OnInspectorGUI()
    {
        var extendedNetworkManager = target as ExtendedNetworkManager;
        void SetExpanded(bool expanded) { extendedNetworkManager.ExtendedNetworkManagerExpanded = expanded; }
        ;
        DrawFoldOutGroup<ExtendedNetworkManager>(extendedNetworkManager.GetType(), DisplayExtendedNetworkManagerProperties, extendedNetworkManager.ExtendedNetworkManagerExpanded, SetExpanded);
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
    // Inspector view expand/collapse settings for this derived child class
    [HideInInspector]
    public bool ExtendedNetworkManagerExpanded;
    protected override void OnValidateComponent()
    {
        m_OriginalVSyncCount = QualitySettings.vSyncCount;
        CheckServiceStatus();
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
    public bool UseRelayConnection = true;

    private Allocation m_Allocation;
    private string m_RelayJoinCode;

    [HideInInspector]
    [SerializeField]
    private int m_OriginalVSyncCount;

    private enum ConnectionStates
    {
        None,
        Connecting,
        Connected,
    }

    private ConnectionStates m_ConnectionState;

    [SerializeField]
    private bool m_ServicesRegistered;
    private ISession m_CurrentSession;
    private string m_SessionName;
    private string m_ProfileName;
    private Task m_SessionTask;
    private SceneLoader m_SceneLoader;

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

    private void Awake()
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
    }

    private async void Start()
    {
        OnClientConnectedCallback += OnClientConnected;
        OnClientDisconnectCallback += OnClientDisconnect;
        OnConnectionEvent += OnClientConnectionEvent;

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
        if (DistributedAuthorityMode && CMBServiceConnection)
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
            if (!m_ServicesRegistered)
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
                    m_SessionName = GUILayout.TextField(m_SessionName);
                    if (GUILayout.Button("Create or Connect To Session"))
                    {
                        NetworkConfig.UseCMBService = true;
                        OnClientStopped += SessionStopped;
                        OnClientStarted += SessionStarted;
                        StartOrConnectToDistributedAuthoritySession();
                    }
                }
            }
        }
        else
        {
            var startText = "Start";
            if (UseRelayConnection)
            {
                startText += " Relay";
                GUILayout.Label("Join Code:", GUILayout.Width(100));
                m_RelayJoinCode = GUILayout.TextField(m_RelayJoinCode);
            }
            if (!UseRelayConnection && GUILayout.Button($"{startText} Server"))
            {
                OnServerStopped += SessionStopped;
                OnServerStarted += SessionStarted;
                StartServer();
            }
            else
            if (GUILayout.Button($"{startText} Host"))
            {
                OnServerStopped += SessionStopped;
                OnServerStarted += SessionStarted;
                if (UseRelayConnection)
                {
                    StartHostWithRelay();
                }
                else
                {
                    StartHost();
                }
            }
            else
            if (GUILayout.Button($"{startText} Client"))
            {
                OnClientStopped += SessionStopped;
                OnClientStarted += SessionStarted;
                if (UseRelayConnection)
                {
                    StartClientWithRelay();
                }
                else
                {
                    StartClient();
                }
            }
        }
        GUILayout.EndArea();
    }

    private async void StartHostWithRelay(int maxConnections = 15)
    {
        m_ConnectionState = ConnectionStates.Connecting;
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        m_Allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        var unityTransport = NetworkConfig.NetworkTransport as UnityTransport;
        unityTransport.UseEncryption = true;
        var defaultEndPoint = (RelayServerEndpoint)null;
        foreach (var endPoint in m_Allocation.ServerEndpoints)
        {
            if (endPoint.Secure && endPoint.Network == RelayServerEndpoint.NetworkOptions.Udp)
            {
                defaultEndPoint = endPoint;
                break;
            }
        }
        m_RelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(m_Allocation.AllocationId);
        unityTransport.SetRelayServerData(defaultEndPoint.Host, (ushort)defaultEndPoint.Port, m_Allocation.AllocationIdBytes, m_Allocation.Key, m_Allocation.ConnectionData, null, defaultEndPoint.Secure);
        StartHost();

    }

    private async void StartClientWithRelay()
    {
        m_ConnectionState = ConnectionStates.Connecting;
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(m_RelayJoinCode);
        var defaultEndPoint = (RelayServerEndpoint)null;
        foreach (var endPoint in joinAllocation.ServerEndpoints)
        {
            if (endPoint.Secure && endPoint.Network == RelayServerEndpoint.NetworkOptions.Udp)
            {
                defaultEndPoint = endPoint;
                break;
            }
        }
        //Populate the joining data
        var unityTransport = NetworkConfig.NetworkTransport as UnityTransport;
        unityTransport.UseEncryption = true;
        unityTransport.SetClientRelayData(defaultEndPoint.Host, (ushort)defaultEndPoint.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData, defaultEndPoint.Secure);
        StartClient();
    }

    private int OnUpdateGUIConnected(int yAxisOffset)
    {
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
            GUILayout.Label($"Client-Server Session");
            if (UseRelayConnection && IsHost)
            {
                GUILayout.Label($"Join Code: {m_RelayJoinCode} (CTRL-C Copy)");
                if (!string.IsNullOrEmpty(m_RelayJoinCode) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
                {
                    GUIUtility.systemCopyBuffer = m_RelayJoinCode;
                }
            }
        }
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(Display.main.renderingWidth - 230, 10, 220, 300));
        var endSessionText = IsServer && !DistributedAuthorityMode ? "Shutdown" : "Disconnect";
        if (GUILayout.Button(endSessionText))
        {
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
            if (IsSessionAuthority())
            {
                var buttonText = $"Load {m_SceneLoader.GetNextSceneNameToLoad()}";
                if (GUILayout.Button(buttonText))
                {
                    m_SceneLoader.LoadNextScene();
                }
            }
        }

        GUILayout.EndArea();

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
                    if (UseRelayConnection && IsHost)
                    {
                        yAxisOffset = OnUpdateGUIConnected(64);
                    }
                    else
                    {
                        yAxisOffset = OnUpdateGUIConnected(40);
                    }
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

    public bool IsSceneEventInProgress()
    {
        return m_SceneLoader != null ? m_SceneLoader.SceneLoadingInProgress : false;
    }

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

    private void Update()
    {
        UpdateRuntimeMessageConsole();
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

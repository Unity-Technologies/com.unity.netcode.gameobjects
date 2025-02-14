using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using SessionState = Unity.Services.Multiplayer.SessionState;

#if UNITY_EDITOR
using Unity.Netcode.Editor;
using UnityEditor;

/// <summary>
/// The custom editor for the <see cref="ExtendedNetworkManager"/> component.
/// </summary>
[CustomEditor(typeof(ExtendedNetworkManager), true)]
[CanEditMultipleObjects]
public class ExtendedNetworkManagerEditor : NetworkManagerEditor
{
    private SerializedProperty m_ConnectionType;
    private SerializedProperty m_TargetFrameRate;
    private SerializedProperty m_EnableVSync;

    public override void OnEnable()
    {
        m_ConnectionType = serializedObject.FindProperty(nameof(ExtendedNetworkManager.ConnectionType));
        m_TargetFrameRate = serializedObject.FindProperty(nameof(ExtendedNetworkManager.TargetFrameRate));
        m_EnableVSync = serializedObject.FindProperty(nameof(ExtendedNetworkManager.EnableVSync));
        base.OnEnable();
    }

    private void DisplayExtendedNetworkManagerProperties()
    {
        EditorGUILayout.PropertyField(m_ConnectionType);
        EditorGUILayout.PropertyField(m_TargetFrameRate);
        EditorGUILayout.PropertyField(m_EnableVSync);
    }

    public override void OnInspectorGUI()
    {
        var extendedNetworkManager = target as ExtendedNetworkManager;
        // Handle switching the appropriate connection type based on the network topology
        // Host connectio type can be set for client-server and distributed authority
        // Live Service can only be used with distributed authority
        // Client-server can only be used with a host connection type
        var connectionTypes = Enum.GetValues(typeof(ExtendedNetworkManager.ConnectionTypes));
        var connectionType = ExtendedNetworkManager.ConnectionTypes.LiveService;
        if (m_ConnectionType.enumValueIndex > 0 && m_ConnectionType.enumValueIndex < connectionTypes.Length)
        {
            connectionType = (ExtendedNetworkManager.ConnectionTypes)connectionTypes.GetValue(m_ConnectionType.enumValueIndex);
        }
        void SetExpanded(bool expanded) { extendedNetworkManager.ExtendedNetworkManagerExpanded = expanded; };
        DrawFoldOutGroup<ExtendedNetworkManager>(extendedNetworkManager.GetType(), DisplayExtendedNetworkManagerProperties, extendedNetworkManager.ExtendedNetworkManagerExpanded, SetExpanded);

        var updatedConnectedType = (ExtendedNetworkManager.ConnectionTypes)connectionTypes.GetValue(m_ConnectionType.enumValueIndex);
        if (connectionType == updatedConnectedType && updatedConnectedType == ExtendedNetworkManager.ConnectionTypes.LiveService && extendedNetworkManager.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer)
        {
            extendedNetworkManager.ConnectionType = ExtendedNetworkManager.ConnectionTypes.Host;
        }
        else if (connectionType == ExtendedNetworkManager.ConnectionTypes.Host && updatedConnectedType == ExtendedNetworkManager.ConnectionTypes.LiveService && extendedNetworkManager.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer)
        {
            extendedNetworkManager.NetworkConfig.NetworkTopology = NetworkTopologyTypes.DistributedAuthority;
        }
        base.OnInspectorGUI();
    }
}
#endif

public enum ConnectionStates
{
    None,
    Connecting,
    Connected,
}

public class ExtendedNetworkManager : NetworkManager
{
#if UNITY_EDITOR
    // Inspector view expand/collapse settings for this derived child class
    [HideInInspector]
    public bool ExtendedNetworkManagerExpanded;
#endif

    public static ExtendedNetworkManager Instance;

    public enum ConnectionTypes
    {
        LiveService,
        Host,
    }
    public ConnectionTypes ConnectionType;

    public int TargetFrameRate = 100;
    public bool EnableVSync = false;

    [HideInInspector]
    [SerializeField]
    private int m_OriginalVSyncCount;

#if UNITY_EDITOR
    protected override void OnValidateComponent()
    {
        m_OriginalVSyncCount = QualitySettings.vSyncCount;
        m_ServicesRegistered = CloudProjectSettings.organizationName != string.Empty && CloudProjectSettings.organizationId != string.Empty;
        base.OnValidateComponent();
    }
#endif
    private ISession m_CurrentSession;

    [SerializeField]
    private bool m_ServicesRegistered;

    private string m_SessionName;
    private string m_ProfileName;
    private Task m_SessionTask;



    private ConnectionStates m_ConnectionState;

    private void UpdateConnectionState(ConnectionStates connectionState)
    {
        m_ConnectionState = connectionState;
        var extensions = s_Extensions.ToArray();
        foreach(var extension in extensions)
        {
            extension.StatusUpdate(connectionState);
        }
    }

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

    private static List<IExtensionHandler> s_Extensions = new List<IExtensionHandler>();

    public static void AttachExtension(IExtensionHandler extendedUpdateHandler)
    {
        s_Extensions.Add(extendedUpdateHandler);

        if (Instance && !extendedUpdateHandler.HasInitialized())
        {
            extendedUpdateHandler.Initialize(Instance);
            extendedUpdateHandler.StatusUpdate(Instance.m_ConnectionState);
        }
    }

    public static void DetachExtension(IExtensionHandler extendedUpdateHandler)
    {
        s_Extensions.Remove(extendedUpdateHandler);
    }

    private void Awake()
    {
        Screen.SetResolution((int)(Screen.currentResolution.width * 0.40f), (int)(Screen.currentResolution.height * 0.40f), FullScreenMode.Windowed);
        SetFrameRate(TargetFrameRate, EnableVSync);
        SetSingleton();
    }

    private async void Start()
    {
        OnClientConnectedCallback += OnClientConnected;
        OnClientDisconnectCallback += OnClientDisconnect;
        OnConnectionEvent += OnClientConnectionEvent;
        // Check to see if the project has been registered with an organization before trying to sign in
        if (m_ServicesRegistered)
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

        foreach(var extension in s_Extensions)
        {
            if (!extension.HasInitialized())
            {
                extension.Initialize(this);
            }
        }
    }

    private void OnDestroy()
    {
        OnClientConnectedCallback -= OnClientConnected;
        OnClientDisconnectCallback -= OnClientDisconnect;
        OnConnectionEvent -= OnClientConnectionEvent;

        for(int i = s_Extensions.Count - 1; i >= 0; i--)
        {
            if (s_Extensions[i].HasInitialized())
            {
                s_Extensions[i].Destroying();
            }
        }
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

    private Rect OnDrawLiveServiceGUI(Rect currentRect)
    {
        var retFieldValues = DrawTextField(currentRect, m_SessionName);
        currentRect = retFieldValues.Item1;
        m_SessionName = retFieldValues.Item2;

        var retButtonValues = DrawButton(currentRect, "Create or Connect To Session");

        if (retButtonValues.Item2)
        {
            currentRect = retButtonValues.Item1;
            NetworkConfig.UseCMBService = true;
            OnClientStopped += ClientStopped;
            OnClientStarted += ClientStarted;
            m_SessionTask = ConnectThroughLiveService();
            UpdateConnectionState(ConnectionStates.Connecting);
            LogMessage($"Connecting to session {m_SessionName}...");
        }

        return currentRect;
    }

    private Rect OnDrawDAHostGUI(Rect currentRect)
    {
        var retValues = DrawButton(currentRect, "Start Host");
        if (retValues.Item2)
        {
            currentRect = retValues.Item1;
            OnClientStopped += ClientStopped;
            OnClientStarted += ClientStarted;
            StartHost();
        }

        retValues = DrawButton(currentRect, "Start Client");
        if (retValues.Item2)
        {
            currentRect = retValues.Item1;
            OnClientStopped += ClientStopped;
            OnClientStarted += ClientStarted;
            StartClient();
        }
        return currentRect;
    }

    private Rect OnUpdateGUIDisconnected(Rect currentRect)
    {
        currentRect = DrawLabel(currentRect, "Session Name");

        var connectionType = ConnectionType;
        if (NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer && connectionType != ConnectionTypes.Host)
        {
            connectionType = ConnectionTypes.Host;
        }

        switch (connectionType)
        {
            case ConnectionTypes.LiveService:
                {
                    currentRect = OnDrawLiveServiceGUI(currentRect);
                    break;
                }
            case ConnectionTypes.Host:
                {
                    currentRect = OnDrawDAHostGUI(currentRect);
                    break;
                }
        }

        var scenesPreloaded = new System.Text.StringBuilder();
        scenesPreloaded.Append("Scenes Preloaded: ");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            scenesPreloaded.Append($"[{scene.name}]");
        }
        currentRect = DrawLabel(currentRect, scenesPreloaded.ToString());
        return currentRect;
    }

    private Rect OnUpdateGUIConnected(Rect currentRect)
    {
        if (CMBServiceConnection)
        {
            currentRect = DrawLabel(currentRect, $"Session: {m_SessionName}");
        }
        else
        {
            if (DistributedAuthorityMode)
            {
                currentRect = DrawLabel(currentRect, $"DAHosted Session");
            }
            else
            {
                currentRect = DrawLabel(currentRect, $"Client-Server Session");
                //if (IsHost)
                //{
                //    GUILayout.Label($"Bracket Keys ([ or ]) change ownership");
                //}
            }
        }
        return currentRect;
    }

    private void OnGUI()
    {
        var topLeftRect = new Rect(10, 10, 800, 800);
        GUILayout.BeginArea(topLeftRect);
        foreach (var extension in s_Extensions)
        {
            topLeftRect = extension.GUIUpdate(topLeftRect, ScreenSpaceRegions.TopLeft);
        }
        switch (m_ConnectionState)
        {
            case ConnectionStates.None:
                {
                    topLeftRect = OnUpdateGUIDisconnected(topLeftRect);
                    break;
                }
            case ConnectionStates.Connected:
                {
                    topLeftRect = OnUpdateGUIConnected(topLeftRect);
                    break;
                }
        }

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

        var currentRect = new Rect(Display.main.renderingWidth - 260, 10, 250, 180);
        GUILayout.BeginArea(currentRect);
        if (m_ConnectionState == ConnectionStates.Connected)
        {
            var retButtonValues = DrawButton(currentRect, "Disconnect");
            if (retButtonValues.Item2)
            {
                currentRect = retButtonValues.Item1;
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
        }

        foreach (var extension in s_Extensions)
        {
            currentRect = extension.GUIUpdate(currentRect, ScreenSpaceRegions.TopRight);
        }

        GUILayout.EndArea();
    }

    protected Rect DrawLabel(Rect currentRect, string msg)
    {
        GUILayout.Label($"{msg}");
        var rect = GUILayoutUtility.GetLastRect();
        currentRect.height += rect.height;
        return currentRect;
    }

    protected (Rect, string) DrawTextField(Rect currentRect, string value)
    {
        value = GUILayout.TextField(value);
        var rect = GUILayoutUtility.GetLastRect();
        currentRect.height += rect.height;
        return (currentRect, value);
    }

    protected (Rect, bool) DrawButton(Rect currentTotalRect, string text, float width = 200)
    {
        var clicked = false;
        if (GUILayout.Button($"{text}", GUILayout.Width(width)))
        {
            var rect = GUILayoutUtility.GetLastRect();
            currentTotalRect.height += rect.height;
            clicked = true;
        }
        return (currentTotalRect,clicked);
    }

    private void ClientStarted()
    {
        OnClientStarted -= ClientStarted;
        UpdateConnectionState(ConnectionStates.Connected);
        LogMessage($"Connected to session {m_SessionName}.");
    }

    private void ClientStopped(bool isHost)
    {
        OnClientStopped -= ClientStopped;
        UpdateConnectionState(ConnectionStates.None);
        m_SessionTask = null;
        m_CurrentSession = null;
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

    public bool IsAuthorityInstance()
    {
        return IsServer || LocalClient.IsSessionOwner;
    }


    private void Update()
    {
        if (m_ConnectionState == ConnectionStates.Connected)
        {
            foreach (var extension in s_Extensions)
            {
                if (IsAuthorityInstance())
                {
                    extension.AuthorityUpdate();
                }
                else
                {
                    extension.NonAuthorityUpdate();
                }
            }
        }

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

    private void OnClientConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        LogMessage($"[{Time.realtimeSinceStartup}] Connection event {eventData.EventType} for Client-{eventData.ClientId}.");
    }

    private void OnClientConnected(ulong clientId)
    {
        LogMessage($"[{Time.realtimeSinceStartup}] Connected event invoked for Client-{clientId}.");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        LogMessage($"[{Time.realtimeSinceStartup}] Disconnected event invoked for Client-{clientId}.");
    }

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

    public ExtendedNetworkManager()
    {
        Instance = this;
    }
}

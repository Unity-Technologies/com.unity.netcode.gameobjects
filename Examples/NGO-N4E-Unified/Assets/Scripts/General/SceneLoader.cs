using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Netcode;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.VectorGraphics.SVGParser;

public class SceneLoader : MonoBehaviour
{
#if UNITY_EDITOR
    public SceneAsset MainMenu;

    [Serializable]
    public class SceneEntryEditor
    {
        public SceneAsset Scene;
        public List<NetworkObject> PlayerPrefabs;
    }
    private List<string> m_SceneNames = new List<string>();
    public List<SceneEntryEditor> NetworkScenesToLoad;
    private void OnValidate()
    {
        m_SceneEntryInfo.Clear();
        m_SceneNames.Clear();
        if (NetworkScenesToLoad != null)
        {
            foreach (SceneEntryEditor sceneEntryEditor in NetworkScenesToLoad)
            {
                if (sceneEntryEditor == null)
                {
                    continue;
                }
                if (sceneEntryEditor.Scene == null)
                {
                    continue;
                }
                if (m_SceneNames.Contains(sceneEntryEditor.Scene.name))
                {
                    sceneEntryEditor.Scene = null;
                    sceneEntryEditor.PlayerPrefabs?.Clear();
                    continue;
                }

                var sceneEntry = new SceneEntryInfo()
                {
                    SceneToLoad = sceneEntryEditor.Scene.name,
                    PlayerPrefabs = new List<NetworkObject>(sceneEntryEditor.PlayerPrefabs),
                };

                m_SceneNames.Add(sceneEntryEditor.Scene.name);
                m_SceneEntryInfo.Add(sceneEntry);
            }
        }
        if (MainMenu)
        {
            m_MainMenuScene = MainMenu.name;
        }
    }
#endif
    [Serializable]
    public class SceneEntryInfo
    {
        // The scene name to load
        public string SceneToLoad;
        // The list of player prefabs to use.
        // Each player prefab is used once per scene loaded event of SceneToLoad.
        public List<NetworkObject> PlayerPrefabs;
    }

    /// <summary>
    /// Primary scene entry used to cycle through scenes and scenes with paired player prefabs
    /// </summary>
    public class SceneEntry
    {
        public SceneEntryInfo SceneInfo { get; private set; }
        public string SceneName { get; private set; }
        public string PlayerPrefabName { get; private set; }
        public bool HasPlayerPrefab { get; private set; }

        /// <summary>
        /// When true and once the associated scene has been loaded, 
        /// iterate to the next <see cref="SceneEntry"/>.
        /// </summary>
        public bool IsLastSceneLoad { get; private set; }

        // Defaults to no entries (-1)
        private int m_PlayerPrefabIndex = -1;

        public NetworkObject GetCurrentPlayerPrefab()
        {
            if (!HasPlayerPrefab || m_PlayerPrefabIndex < 0)
            {
                return null;
            }

            return SceneInfo.PlayerPrefabs[m_PlayerPrefabIndex];
        }

        public NetworkObject GetPlayerPrefabFromGlobalObjectId(uint globalObjectId)
        {
            if (SceneInfo.PlayerPrefabs.Count == 0)
            {
                return null;
            }
            foreach (var prefab in SceneInfo.PlayerPrefabs)
            {
                if (prefab.PrefabIdHash == globalObjectId)
                {
                    return prefab;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the next player prefab to be used
        /// </summary>
        /// <returns>Either the player prefab or none.</returns>
        public string GetNextPlayerPrefabName()
        {
            if (!HasPlayerPrefab)
            {
                return string.Empty;
            }

            if ((m_PlayerPrefabIndex + 1) >= SceneInfo.PlayerPrefabs.Count)
            {
                return string.Empty;
            }

            return SceneInfo.PlayerPrefabs[m_PlayerPrefabIndex + 1].name;
        }

        /// <summary>
        /// Invoked every time the associated scene is loaded.
        /// </summary>
        public void SceneLoaded()
        {
            if (IsLastSceneLoad)
            {
                return;
            }
            UpdatePlayerPrefabIndex();
        }

        /// <summary>
        /// Resets this entry to be re-used again.
        /// </summary>
        public void Reset()
        {
            m_PlayerPrefabIndex = -1;
            IsLastSceneLoad = false;
            if (SceneInfo.PlayerPrefabs.Count > 0)
            {
                PlayerPrefabName = SceneInfo.PlayerPrefabs[0].name;
            }
        }

        private void UpdatePlayerPrefabIndex()
        {
            if (!HasPlayerPrefab)
            {
                IsLastSceneLoad = true;
            }

            m_PlayerPrefabIndex++;
            PlayerPrefabName = SceneInfo.PlayerPrefabs[m_PlayerPrefabIndex].name;
            IsLastSceneLoad = (m_PlayerPrefabIndex + 1) >= SceneInfo.PlayerPrefabs.Count;
        }

        public SceneEntry(SceneEntryInfo sceneEntryInfo)
        {
            SceneInfo = sceneEntryInfo;
            HasPlayerPrefab = SceneInfo.PlayerPrefabs != null && SceneInfo.PlayerPrefabs.Count > 0;
            SceneName = SceneInfo.SceneToLoad;
        }
    }

    public bool SceneLoadingInProgress { get; private set; }

    [HideInInspector]
    [SerializeField]
    private string m_MainMenuScene;

    private Dictionary<string, int> m_SceneEntriesTable = new Dictionary<string, int>();
    private List<SceneEntry> m_SceneEntries = new List<SceneEntry>();


    [HideInInspector]
    [SerializeField]
    private List<SceneEntryInfo> m_SceneEntryInfo = new List<SceneEntryInfo>();


    private ExtendedNetworkManager m_NetworkManager;
    private Scene m_CurrentLoadedScene;

    private void Awake()
    {
        m_NetworkManager = GetComponent<ExtendedNetworkManager>();
    }

    private void Start()
    {
        m_SceneEntriesTable.Clear();
        for (int i = 0; i < m_SceneEntryInfo.Count; i++)
        {
            var entry = m_SceneEntryInfo[i];
            var sceneEntry = new SceneEntry(m_SceneEntryInfo[i]);
            m_SceneEntriesTable.Add(entry.SceneToLoad, i);
            m_SceneEntries.Add(sceneEntry);
        }
    }

    private enum SceneLoaderState
    {
        PreLoading,
        Loaded,
        Synchronized
    }

    public SceneEntry CurrentSceneEntry => GetCurrentSceneEntry();

    private string m_PreviousSceneName;
    private SceneEntry GetCurrentSceneEntry(bool usePrevious = false)
    {
        var sceneName = usePrevious ? m_PreviousSceneName : m_CurrentLoadedScene.name;
        if (string.IsNullOrEmpty(sceneName))
        {
            if (usePrevious && string.IsNullOrEmpty(sceneName))
            {
                return null;
            }
            else if (!usePrevious)
            {
                return null;
            }
        }
        if (!m_SceneEntriesTable.ContainsKey(sceneName))
        {
            return null;
        }
        return m_SceneEntries[m_SceneEntriesTable[sceneName]];
    }

    /// <summary>
    /// Handles scene entry progression and tracking.
    /// </summary>
    /// <param name="loaderState">Type of update happening (i.e. what invoked it).</param>
    /// <param name="clientId">Only set when a client is synchronized to handle late joining clients.</param>
    private void UpdateCurrentScene(SceneLoaderState loaderState, ulong clientId = ulong.MaxValue)
    {
        var currentEntry = GetCurrentSceneEntry(true);
        m_CurrentLoadedScene = SceneManager.GetActiveScene();
        m_PreviousSceneName = m_CurrentLoadedScene.name;
        if (loaderState == SceneLoaderState.PreLoading || CurrentSceneEntry == null)
        {
            return;
        }

        // Only switch to the next scene entry when the current one is considered complete
        if (loaderState == SceneLoaderState.Loaded)
        {
            if (currentEntry != null && currentEntry != CurrentSceneEntry)
            {
                // Reset this scene entry for use next time aroujnd (i.e. round robin)
                currentEntry.Reset();
            }
            CurrentSceneEntry.SceneLoaded();
        }

        if (!m_NetworkManager.IsServer && !m_NetworkManager.DistributedAuthorityMode)
        {
            return;
        }

        var playerPrefab = CurrentSceneEntry.GetCurrentPlayerPrefab();
        var isSynchronize = loaderState == SceneLoaderState.Synchronized;
        var playerObject = (isSynchronize && clientId != ulong.MaxValue) ? m_NetworkManager.ConnectedClients[clientId].PlayerObject : m_NetworkManager.LocalClient.PlayerObject;
        if (playerPrefab != null && (playerObject != null && playerObject.PrefabIdHash != playerPrefab.PrefabIdHash))
        {
            if (m_NetworkManager.DistributedAuthorityMode)
            {
                m_NetworkManager.LocalClient.PlayerObject?.Despawn();
                m_NetworkManager.LocalClient.PlayerObject = NetworkObject.InstantiateAndSpawn(playerPrefab.gameObject, m_NetworkManager, m_NetworkManager.LocalClientId, false, true, false, playerPrefab.transform.position, playerPrefab.transform.rotation);
            }
            else if (m_NetworkManager.IsServer)
            {
                if (loaderState == SceneLoaderState.Synchronized)
                {
                    // Only if a client id is specified
                    if (clientId != ulong.MaxValue)
                    {
                        // Spawn the player prefab specific to this SceneEntry loaded state
                        var synchronizedClient = m_NetworkManager.ConnectedClients[clientId];
                        synchronizedClient.PlayerObject?.Despawn(true);
                        synchronizedClient.PlayerObject = NetworkObject.InstantiateAndSpawn(playerPrefab.gameObject, m_NetworkManager, clientId, false, true, false, playerPrefab.transform.position, playerPrefab.transform.rotation);
                    }
                }
                else
                if (loaderState == SceneLoaderState.Loaded)
                {
                    // Spawn new player prefabs for all connected clients
                    foreach (var connectedClient in m_NetworkManager.ConnectedClients)
                    {
                        connectedClient.Value.PlayerObject?.Despawn(true);
                        connectedClient.Value.PlayerObject = NetworkObject.InstantiateAndSpawn(playerPrefab.gameObject, m_NetworkManager, connectedClient.Key, false, true, false, playerPrefab.transform.position, playerPrefab.transform.rotation);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Invoked when the NetworkManager is started
    /// </summary>
    private void OnStarted()
    {
        m_NetworkManager.OnClientStarted -= ClientStarted;
        m_NetworkManager.OnServerStarted -= ServerStarted;
        m_NetworkManager.OnServerStopped += OnServerStopped;
        m_NetworkManager.OnClientStopped += OnClientStopped;
        m_NetworkManager.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;
        if (m_NetworkManager.DistributedAuthorityMode || m_NetworkManager.IsServer)
        {
            m_NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }
        else
        {
            m_NetworkManager.SceneManager.OnLoadComplete += OnLoadComplete;
        }

        // Session authority is either the server or session owner
        if (m_NetworkManager.IsSessionAuthority())
        {
            LoadNextScene();
        }
    }

    /// <summary>
    /// Invoked by clients
    /// </summary>
    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (m_NetworkManager.IsServer)
        {
            Debug.LogError("Server should never be invoking this!!!");
            return;
        }
        UpdateCurrentScene(SceneLoaderState.Loaded);


        m_NetworkManager.LogMessage($"Scene loading event for {sceneName} completed!");
    }

    /// <summary>
    /// Invoked by the server or da clients
    /// </summary>
    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        SceneLoadingInProgress = false;
        UpdateCurrentScene(SceneLoaderState.Loaded);
        var completed = string.Join(",", clientsCompleted);
        var timedOut = string.Join(",", clientsTimedOut);
        m_NetworkManager.LogMessage($"Scene loading event for {sceneName} completed!\n Client ids that completed event ({completed}) \n Client ids that timed out ({timedOut})");
    }

    /// <summary>
    /// Handle spawning the correct player prefab for this scene
    /// </summary>
    /// <param name="clientId"></param>
    private void OnSynchronizeComplete(ulong clientId)
    {
        m_NetworkManager.LogMessage($"Client-{clientId} synchronization completed.");

        // TODO (Fix me?): This won't work for distributed authority
        if (!m_NetworkManager.IsServer)
        {
            return;
        }
        UpdateCurrentScene(SceneLoaderState.Synchronized, clientId);
    }

    public void DAClientStarted()
    {
        // Use additive client synchronization.
        m_NetworkManager.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
        // Unload any scenes not used once the client has finished synchronizing.
        m_NetworkManager.SceneManager.PostSynchronizationSceneUnloading = true;
        LoadNextScene(true);
    }

    private void ClientStarted()
    {
        OnStarted();
    }

    private void ServerStarted()
    {
        if (m_NetworkManager.IsHost)
        {
            return;
        }
        OnStarted();
    }

    public void LoadMainMenu()
    {
        if (m_MainMenuScene != string.Empty)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(m_MainMenuScene, LoadSceneMode.Single);
        }
    }
    private void OnSceneLoaded(Scene sceneLoaded, LoadSceneMode loadSceneMode)
    {
        if (sceneLoaded.name == m_MainMenuScene)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            m_NetworkManager.OnServerStopped -= OnServerStopped;
            m_NetworkManager.OnClientStopped -= OnClientStopped;
            m_NetworkManager.OnClientStarted += ClientStarted;
            m_NetworkManager.OnServerStarted += ServerStarted;
        }
    }

    private void OnStopped()
    {
        // Reset all scene entries for next session
        foreach (var sceneEntry in m_SceneEntries)
        {
            sceneEntry.Reset();
        }
        LoadMainMenu();
    }

    private void OnServerStopped(bool wasHost)
    {
        if (!wasHost)
        {
            OnStopped();
        }
    }

    private void OnClientStopped(bool wasHost)
    {
        OnStopped();
    }

    private string m_NextSceneLoading;
    public Action OnSceneLoadStart;

    /// <summary>
    /// Loads the next scene and is invoked when the load scene button is clicked.
    /// </summary>
    /// <param name="isPreload">For the main menu prior to loading any scenes synchronized.</param>
    public void LoadNextScene(bool isPreload = false)
    {
        if ((!m_NetworkManager.DistributedAuthorityMode && !m_NetworkManager.IsServer) || !m_NetworkManager.IsListening)
        {
            return;
        }

        m_NextSceneLoading = GetNextSceneToLoad();
        if (isPreload)
        {
            SceneManager.sceneLoaded += OnPreSceneLoaded;
            SceneManager.LoadScene(m_NextSceneLoading, LoadSceneMode.Single);
            SceneLoadingInProgress = true;
        }
        else
        {
            OnSceneLoadStart?.Invoke();
            var loadStatus = m_NetworkManager.SceneManager.LoadScene(m_NextSceneLoading, LoadSceneMode.Single);
            if (loadStatus != SceneEventProgressStatus.Started)
            {
                m_NetworkManager.LogMessage($"Failed to start scene loading event for {m_NextSceneLoading}! [Status: {loadStatus}]");
            }
            else
            {
                SceneLoadingInProgress = true;
            }
        }
    }

    private void OnPreSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name == m_NextSceneLoading)
        {
            SceneManager.sceneLoaded -= OnPreSceneLoaded;
            SceneLoadingInProgress = false;
            UpdateCurrentScene(SceneLoaderState.PreLoading);
        }
    }

    /// <summary>
    /// Gets the next scene to be loaded.
    /// If there are more than one player prefab to use on this scene then the same scene will be returned.
    /// Look at <see cref="UpdateCurrentScene(SceneLoaderState, ulong)"/> for more information on how the
    /// player prefab list is incremented.
    /// </summary>
    private string GetNextSceneToLoad()
    {
        if (CurrentSceneEntry != null)
        {
            if (CurrentSceneEntry.IsLastSceneLoad)
            {
                // This prepares this entry for the next time it is the current (i.e. round robin)
                return m_SceneEntries[(m_SceneEntries.IndexOf(CurrentSceneEntry) + 1) % m_SceneEntries.Count].SceneName;
            }
            else
            {
                return CurrentSceneEntry.SceneName;
            }
        }

        if (m_SceneEntries != null && m_SceneEntries.Count > 0)
        {
            return m_SceneEntries[0].SceneInfo.SceneToLoad;
        }

        return string.Empty;
    }

    /// <summary>
    /// Returns back the next scne that will be loaded
    /// </summary>
    public string GetNextSceneName()
    {
        if (CurrentSceneEntry != null)
        {
            if (!CurrentSceneEntry.IsLastSceneLoad)
            {
                return CurrentSceneEntry.SceneName;
            }
            return m_SceneEntries[(m_SceneEntries.IndexOf(CurrentSceneEntry) + 1) % m_SceneEntries.Count].SceneName;
        }

        if (m_SceneEntries != null && m_SceneEntries.Count > 0)
        {
            return m_SceneEntries[0].SceneInfo.SceneToLoad;
        }

        return string.Empty;
    }

    /// <summary>
    /// Returns back the current player prefab spawned for the local client
    /// </summary>
    public string GetCurrentPrefabName()
    {
        if (m_NetworkManager.IsSessionAuthority() && CurrentSceneEntry != null)
        {
            return CurrentSceneEntry.PlayerPrefabName;
        }

        if (CurrentSceneEntry != null && m_NetworkManager != null && m_NetworkManager.LocalClient != null && m_NetworkManager.LocalClient.PlayerObject != null)
        {
            var prefab = CurrentSceneEntry.GetPlayerPrefabFromGlobalObjectId(m_NetworkManager.LocalClient.PlayerObject.PrefabIdHash);
            if (prefab != null)
            {
                return prefab.name;
            }
        }

        if (m_NetworkManager != null && m_NetworkManager.NetworkConfig.PlayerPrefab != null)
        {
            return m_NetworkManager.NetworkConfig.PlayerPrefab.name;
        }

        return string.Empty;
    }

    /// <summary>
    /// Returns back the next player prefab to be used in the next scene being loaded
    /// </summary>
    public string GetNextPrefabName()
    {
        if (CurrentSceneEntry != null)
        {
            if (!CurrentSceneEntry.IsLastSceneLoad)
            {
                return CurrentSceneEntry.GetNextPlayerPrefabName();
            }
            return m_SceneEntries[(m_SceneEntries.IndexOf(CurrentSceneEntry) + 1) % m_SceneEntries.Count].GetNextPlayerPrefabName();
        }

        if (m_NetworkManager != null && m_NetworkManager.NetworkConfig.PlayerPrefab != null)
        {
            return m_NetworkManager.NetworkConfig.PlayerPrefab.name;
        }

        return string.Empty;
    }
}

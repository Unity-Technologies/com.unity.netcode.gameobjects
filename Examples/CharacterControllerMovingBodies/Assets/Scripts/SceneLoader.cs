using System.Collections.Generic;
using Unity.Netcode;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
#if UNITY_EDITOR
    public SceneAsset MainMenu;
    public List<SceneAsset> NetworkScenesToLoad;
    private void OnValidate()
    {
        if (NetworkScenesToLoad != null)
        {
            foreach (SceneAsset sceneAsset in NetworkScenesToLoad)
            {
                if (sceneAsset == null) continue;
                m_SceneNamesToLoad.Add(sceneAsset.name);
            }
        }
        if (MainMenu)
        {
            m_MainMenuScene = MainMenu.name;
        }
    }
#endif
    public bool SceneLoadingInProgress { get; private set; }
    
    [HideInInspector]
    [SerializeField]
    private string m_MainMenuScene;
    [HideInInspector]
    [SerializeField]
    private List<string> m_SceneNamesToLoad = new List<string>();

    private ExtendedNetworkManager m_NetworkManager;
    private Scene m_CurrentLoadedScene;
    private int m_NextSceneNameIndex = 0;

    private void Awake()
    {
        m_NetworkManager = GetComponent<ExtendedNetworkManager>();
    }

    private void SetCurrentScene()
    {
        m_CurrentLoadedScene = SceneManager.GetActiveScene();
        if (m_SceneNamesToLoad.Contains(m_CurrentLoadedScene.name)) 
        {
            m_NextSceneNameIndex = (m_SceneNamesToLoad.IndexOf(m_CurrentLoadedScene.name) + 1) % m_SceneNamesToLoad.Count; 
        }
        else
        {
            m_NextSceneNameIndex = 0;
        }
    }

    private void OnStarted()
    {
        m_NetworkManager.OnClientStarted -= ClientStarted;
        m_NetworkManager.OnServerStarted -= ServerStarted;
        m_NetworkManager.OnServerStopped += OnServerStopped;
        m_NetworkManager.OnClientStopped += OnClientStopped;
        m_NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        m_NetworkManager.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;
        if (m_NetworkManager.IsServer)
        {
            LoadNextScene();
        }
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        SceneLoadingInProgress = false;
        SetCurrentScene();
        var completed = string.Join(",", clientsCompleted);
        var timedOut = string.Join(",", clientsTimedOut);
        m_NetworkManager.LogMessage($"Scene loading event for {sceneName} completed!\n Client ids that completed event ({completed}) \n Client ids that timed out ({timedOut})");
    }


    private void OnSynchronizeComplete(ulong clientId)
    {
        m_NetworkManager.LogMessage($"Client-{clientId} synchronization completed.");
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
            SetCurrentScene();
        }
    }

    private void OnStopped()
    {
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
    public void LoadNextScene(bool isPreload = false)
    {
        if ((!m_NetworkManager.DistributedAuthorityMode && !m_NetworkManager.IsServer) || !m_NetworkManager.IsListening) 
        {
            return;
        }

        m_NextSceneLoading = GetNextSceneNameToLoad();
        if (isPreload)
        {
            SceneManager.sceneLoaded += OnPreSceneLoaded;
            SceneManager.LoadScene(m_NextSceneLoading, LoadSceneMode.Single); 
            SceneLoadingInProgress = true;
        }
        else
        {
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
            SetCurrentScene();
        }
    }

    public string GetNextSceneNameToLoad()
    {
        return m_SceneNamesToLoad[m_NextSceneNameIndex];
    }
}

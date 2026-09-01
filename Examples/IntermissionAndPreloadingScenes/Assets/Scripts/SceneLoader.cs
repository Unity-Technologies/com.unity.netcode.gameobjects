using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Serializable]
    public class SceneEntry : IEquatable<SceneEntry>
    {
#if UNITY_EDITOR
        public SceneAsset Scene;
#endif
        public bool UseSceneLoadDelay;
        [HideInInspector]
        public string SceneName;

        public void OnValidate()
        {
            if (Scene)
            {
                SceneName = Scene.name;
            }
        }

        public bool Equals(SceneEntry other)
        {
            if (other.SceneName == SceneName)
            {
                return true;
            }
            return false;
        }
    }

    public SceneEntry MainMenu;
    public List<SceneEntry> NetworkScenesToLoad;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (NetworkScenesToLoad != null)
        {
            foreach (SceneEntry sceneEntry in NetworkScenesToLoad)
            {
                sceneEntry.OnValidate();
            }
        }

        MainMenu?.OnValidate();
    }
#endif
    public bool UseIntermissionWhileLoading;

    [SerializeField]
    private SceneIntermission m_SceneIntermission;

    public bool SceneLoadingInProgress { get; private set; }


    private ExtendedNetworkManager m_NetworkManager;
    private Scene m_CurrentLoadedScene;
    private int m_NextSceneNameIndex = 0;

    private void Awake()
    {
        m_NetworkManager = GetComponent<ExtendedNetworkManager>();
    }

    public void SetCurrentScene()
    {
        m_CurrentLoadedScene = SceneManager.GetActiveScene();
        var validEntries = NetworkScenesToLoad.Where((c) => c.SceneName == m_CurrentLoadedScene.name); xa
        if (validEntries.Any())
        {
            var validScene = validEntries.First();
            m_NextSceneNameIndex = (NetworkScenesToLoad.IndexOf(validScene) + 1) % NetworkScenesToLoad.Count;

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
        m_SceneIntermission = FindFirstObjectByType<SceneIntermission>();
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        SceneLoadingInProgress = false;
        SetCurrentScene();
        var completed = string.Join(",", clientsCompleted);
        var timedOut = string.Join(",", clientsTimedOut);
        m_NetworkManager.LogMessage($"Scene loading event for {sceneName} completed!\n Client ids that completed event ({completed}) \n Client ids that timed out ({timedOut})");
        if (m_SceneIntermission.IntermissionIsActive)
        {
            m_SceneIntermission.EndIntermission();
        }
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
        if (MainMenu != null && MainMenu.SceneName != string.Empty)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(MainMenu.SceneName, LoadSceneMode.Single);
        }
    }
    private void OnSceneLoaded(Scene sceneLoaded, LoadSceneMode loadSceneMode)
    {
        if (sceneLoaded.name == MainMenu.SceneName)
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
            if (UseIntermissionWhileLoading && m_SceneIntermission)
            {
                m_SceneIntermission.BeginLoadingScene(m_NextSceneLoading);
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

    public bool IsIntermissionActive()
    {
        return m_SceneIntermission.IntermissionIsActive;
    }

    public string GetNextSceneNameToLoad()
    {
        return NetworkScenesToLoad[m_NextSceneNameIndex].SceneName;
    }

    public bool ShouldDelayFinalSceneLoad(string sceneName)
    {
        var validScene = NetworkScenesToLoad.Where((c) => c.SceneName == sceneName).First();
        if (validScene != null)
        {
            return validScene.UseSceneLoadDelay;
        }
        return false;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class SceneTransitionHandler : NetworkBehaviour
{
    static public SceneTransitionHandler sceneTransitionHandler { get; internal set; }

    [SerializeField]
    public string DefaultMainMenu = "StartMenu";

    [HideInInspector]
    public delegate void ClientLoadedSceneDelegateHandler(ulong clientId);
    [HideInInspector]
    public event ClientLoadedSceneDelegateHandler OnClientLoadedScene;

    [HideInInspector]
    public delegate void SceneStateChangedDelegateHandler(SceneStates newState);
    [HideInInspector]
    public event SceneStateChangedDelegateHandler OnSceneStateChanged;

    private int m_numberOfClientLoaded;
    
    /// <summary>
    /// Example scene states
    /// </summary>
    public enum SceneStates
    {
        Init,
        Start,
        Lobby,
        Ingame
    }

    private SceneStates m_SceneState;

    /// <summary>
    /// Awake
    /// If another version exists, destroy it and use the current version
    /// Set our scene state to INIT
    /// </summary>
    private void Awake()
    {
        if(sceneTransitionHandler != this && sceneTransitionHandler != null)
        {
            GameObject.Destroy(sceneTransitionHandler.gameObject);
        }
        sceneTransitionHandler = this;
        SetSceneState(SceneStates.Init);
        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// SetSceneState
    /// Sets the current scene state to help with transitioning.
    /// </summary>
    /// <param name="sceneState"></param>
    public void SetSceneState(SceneStates sceneState)
    {
        m_SceneState = sceneState;
        if(OnSceneStateChanged != null)
        {
            OnSceneStateChanged.Invoke(m_SceneState);
        }
    }

    /// <summary>
    /// GetCurrentSceneState
    /// Returns the current scene state
    /// </summary>
    /// <returns>current scene state</returns>
    public SceneStates GetCurrentSceneState()
    {
        return m_SceneState;
    }

    /// <summary>
    /// Start
    /// Loads the default main menu when started (this should always be a component added to the networking manager)
    /// </summary>
    private void Start()
    {
        if(m_SceneState == SceneStates.Init)
        {
            SceneManager.LoadScene(DefaultMainMenu);
        }
    }

    /// <summary>
    /// Registers callbacks to the NetworkSceneManager. This should be called when starting the server
    /// </summary>
    public void RegisterCallbacks()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
        
    }

    /// <summary>
    /// Switches to a new scene
    /// </summary>
    /// <param name="scenename"></param>
    public void SwitchScene(string scenename)
    {
        if(NetworkManager.Singleton.IsListening)
        {
            m_numberOfClientLoaded = 0;
            NetworkManager.Singleton.SceneManager.LoadScene(scenename, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadSceneAsync(scenename);
        }
    }

    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        m_numberOfClientLoaded += 1;
        OnClientLoadedScene?.Invoke(clientId);
    }

    public bool AllClientsAreLoaded()
    {
        return m_numberOfClientLoaded == NetworkManager.Singleton.ConnectedClients.Count;
    }

    /// <summary>
    /// ExitAndLoadStartMenu
    /// This should be invoked upon a user exiting a multiplayer game session.
    /// </summary>
    public void ExitAndLoadStartMenu()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
        OnClientLoadedScene = null;
        SetSceneState(SceneStates.Start);
        SceneManager.LoadScene(1);
    }
}
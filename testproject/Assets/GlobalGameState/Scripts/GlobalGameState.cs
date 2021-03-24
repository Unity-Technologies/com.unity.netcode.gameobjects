using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.SceneManagement;
using MLAPIGlobalGameState;


public class GlobalGameState : NetworkBehaviour
{
    static public GlobalGameState Singleton { get; internal set; }

    [Header("Window Resolution")]

    [SerializeField]
    [Tooltip("Is the game going start in full screen mode?")]
    private bool m_IsFullScreen;

    [SerializeField]
    [Tooltip("If set, the user cannot hit <alt><enter> and get into windowed mode.")]
    private bool m_IsExclusive;

    [SerializeField]
    [Tooltip("If set, the maximum resolution will be used (HorizontalResolution and VerticalResolution will be ignored).")]
    private bool m_MaximumResolution;

    [SerializeField]
    [Tooltip("Horizontal window resolution size (if MaximumResolution this is ignored)")]
    private int m_HorizontalResolution = 1024;

    [SerializeField]
    [Tooltip("Vertical window resolution size (if MaximumResolution this is ignored)")]
    private int m_VerticalResolution = 768;

    [Space]
    /// <summary>
    /// The name of the scene that is the default main menu
    /// </summary>
    [SerializeField]
    private StateToSceneTransitionList m_SceneToStateLinks;


    [SerializeField]
    private GameObject m_GlobalGameStatePrefabObject;

    #region Events and Delegate Handlers
    [HideInInspector]
    public delegate void ClientLoadedSceneDelegateHandler(ulong clientId);

    /// <summary>
    /// clientLoadedScene (Event)
    /// Usage:  typically the host-server subscribes to this event to be notified
    /// when each individual player has loaded a scene
    /// </summary>
    [HideInInspector]
    public event ClientLoadedSceneDelegateHandler ClientLoadedScene;

    [HideInInspector]
    public delegate void GameStateChangedDelegateHandler(GameStates previousState, GameStates newState);

    /// <summary>
    /// gameStateChanged (Event)
    /// Usage: everyone (clients and serve) can subscribe to this event
    /// to be notified when the global game state changes
    /// </summary>
    [HideInInspector]
    public event GameStateChangedDelegateHandler GameStateChanged;

    [HideInInspector]
    public delegate void AllPlayersLoadedSceneDelegateHandler();

    /// <summary>
    /// allPlayersLoadedScene (Event)
    /// Usage: typically the host-server subscribes to this event to be notified
    /// that all players have finished loading a scene
    /// </summary>
    [HideInInspector]
    public event AllPlayersLoadedSceneDelegateHandler AllPlayersLoadedScene;
    #endregion

    /// <summary>
    /// The GlobalGameState component used to synchronize changes
    /// from the server when multi-player session is active
    /// </summary>
    private GlobalGameStateComponent m_GlobalGameStateComponent;

    /// <summary>
    /// The GlobalGameState GameObject that is assigned the GlobalGameStateComponent
    /// </summary>
    private GameObject m_GlobalGameStateGameObject;

    /// <summary>
    /// The global game state
    /// This is only synchronized when in a multi-player session
    /// </summary>
    private GameStates m_GameState;

    //This is set prior to setting any game state that activates MLAPI
    [HideInInspector]
    public bool IsHostingGame;

    //Used to determine the current state of MLAPI (Global Game State Relative)
    private StateToSceneTransitionLinks.MLAPIStates m_CurrentMLAPIState;

    //Tracks our scene switching progress
    private SceneSwitchProgress m_SceneProgress;

    private int m_MLAPIStateSceneIndex;

    /// <summary>
    /// GameStates
    /// Relates to:  NetworkVariable<GameStates> m_GameState
    /// Provides the different states for the networked game state machine
    /// </summary>
    public enum GameStates
    {
        None,               //The initial state
        Init,               //Optional game intro screen, video, or additional splash screens
        Menu,               //When the user has reached the main menu to the game
        Lobby,              //When the user has entered a game session lobby
        InGame,             //When the user has entered into the game session itself
        ExitGame,           //When the user is exiting the game session
        Shutdown            //When the system is shutting down
    }

    /// <summary>
    /// If another version exists, destroy it and use the current version
    /// Set our scene state to INIT
    /// </summary>
    private void Awake()
    {
        if (Singleton != this && Singleton != null)
        {
            GameObject.Destroy(Singleton.gameObject);
        }
        m_MLAPIStateSceneIndex = 0;
        Singleton = this;
    }

    /// <summary>
    /// Kicks of the initialization state, this is where we load the first scene from the MLAPI_BootStrap
    /// </summary>
    private void Start()
    {
        if (!m_IsExclusive)
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        }

        if (m_MaximumResolution)
        {
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, m_IsFullScreen);
        }
        else
        {
            Screen.SetResolution(m_HorizontalResolution, m_VerticalResolution, m_IsFullScreen);
        }

        //Only invoke this when the application is playing (i.e. do not execute this code within the editor in edit mode)
        if (Application.isPlaying)
        {
            if (!CheckForBootStrappedScene())
            {
                //Server and clients always execute this
                SetGameState(GameStates.Init);
            }
        }
    }

    /// <summary>
    /// Clients and Server can register for this in order to synchronize the global game state between all clients (including the host-client)
    /// </summary>
    /// <param name="previousState">from state</param>
    /// <param name="newState">to state</param>
    public void OnGameStateChanged(GameStates previousState, GameStates newState)
    {
        if (NetworkManager.Singleton.IsListening)
        {
            if (GameStateChanged != null)
            {
                GameStateChanged.Invoke(previousState, newState);
            }
        }
    }

    /// <summary>
    /// Used primarily to update anything specific to a Global Game State
    /// </summary>
    private void Update()
    {
        //Any per frame tasks can be performed here (this is not required but can be useful for things like time outs and such)
        switch (m_GameState)
        {
            case GameStates.Init:
                {
                    break;
                }
            case GameStates.Menu:
                {
                    break;
                }
            case GameStates.Lobby:
                {

                    break;
                }
            case GameStates.InGame:
                {
                    break;
                }
            case GameStates.ExitGame:
                {
                    //one level deep recursive call to start loading the main menu again
                    SetGameState(GameStates.Menu);
                    break;
                }
        }
    }

    /// <summary>
    /// Handles the spinning up and shutting down of MLAPI
    /// </summary>
    /// <param name="newState">new state we are transitioning to</param>
    private void UpdateMLAPIState(GameStates newState)
    {
        StateToSceneTransitionLinks.MLAPIStates NewMLAPIState = m_SceneToStateLinks.GetGameStateToMLAPIState(newState);
        if (m_CurrentMLAPIState != NewMLAPIState)
        {
            switch (NewMLAPIState)
            {
                case StateToSceneTransitionLinks.MLAPIStates.InSession:
                case StateToSceneTransitionLinks.MLAPIStates.Connecting:
                    {
                        if (m_CurrentMLAPIState == StateToSceneTransitionLinks.MLAPIStates.None)
                        {
                            if (!NetworkManager.Singleton.IsListening)
                            {
                                //If we are host, then start the host
                                if (IsHostingGame)
                                {
                                    NetworkManager.Singleton.StartHost();  //Spin up the host

                                    //We create a global game state object from our prefab in order to assure
                                    //global game state can be synchronized between server and client(s)
                                    if (m_GlobalGameStatePrefabObject != null)
                                    {
                                        m_GlobalGameStateGameObject = GameObject.Instantiate(m_GlobalGameStatePrefabObject);
                                        m_GlobalGameStateComponent = m_GlobalGameStateGameObject.GetComponent<GlobalGameStateComponent>();
                                        if (m_GlobalGameStateComponent != null && !m_GlobalGameStateComponent.NetworkObject.IsSpawned)
                                        {
                                            m_GlobalGameStateComponent.NetworkObject.Spawn();
                                        }
                                    }
                                }
                                else //otherwise start the client
                                {
                                    NetworkManager.Singleton.StartClient();//Spin up the client
                                }
                            }
                        }
                        break;
                    }
                case StateToSceneTransitionLinks.MLAPIStates.ExitSession:
                    {
                        if (m_CurrentMLAPIState == StateToSceneTransitionLinks.MLAPIStates.InSession || m_CurrentMLAPIState == StateToSceneTransitionLinks.MLAPIStates.Connecting)
                        {
                            if (NetworkManager.Singleton.IsListening)
                            {
                                //If we are host, then stop the host
                                if (IsHostingGame)
                                {
                                    IsHostingGame = false;

                                    //We de-spawn and destroy the global game state object
                                    if (m_GlobalGameStateComponent != null && !m_GlobalGameStateComponent.NetworkObject.IsSpawned)
                                    {
                                        m_GlobalGameStateComponent.NetworkObject.Despawn();
                                    }
                                    if (m_GlobalGameStateGameObject != null)
                                    {
                                        GameObject.Destroy(m_GlobalGameStateGameObject);
                                        m_GlobalGameStateGameObject = null;
                                    }

                                    NetworkManager.Singleton.StopHost();  //shutdown the host

                                }
                                else //otherwise stop the client
                                {
                                    if (m_GlobalGameStateGameObject != null)
                                    {
                                        GameObject.Destroy(m_GlobalGameStateGameObject);
                                        m_GlobalGameStateGameObject = null;
                                    }
                                    NetworkManager.Singleton.StopClient();//shutdown the client
                                }

                                //Clean up scene loading and progress handlers
                                if (m_SceneProgress != null)
                                {
                                    m_SceneProgress = null;
                                }
                                if (ClientLoadedScene != null)
                                {
                                    ClientLoadedScene = null;
                                }
                            }
                        }
                        break;
                    }
            }
            m_CurrentMLAPIState = NewMLAPIState;
        }
    }

    /// <summary>
    /// Any additional processing we might want to do globally depending upon the current state
    /// </summary>
    /// <param name="previousState">from state</param>
    /// <param name="newState">to state</param>
    private void GameStateChangedUpdate(GameStates previousState, GameStates newState)
    {
        var SceneName = string.Empty;
        if (previousState == newState)
        {
            m_MLAPIStateSceneIndex++;
            SceneName = m_SceneToStateLinks.GetSceneNameLinkedToState(newState, m_MLAPIStateSceneIndex);
        }
        else
        {
            SceneName = m_SceneToStateLinks.GetSceneNameLinkedToState(newState);
            if (SceneName != string.Empty)
            {
                m_MLAPIStateSceneIndex = 0;
            }
        }

        //We only should update once per changed state
        if (SceneName != string.Empty)
        {
            if (m_CurrentMLAPIState == StateToSceneTransitionLinks.MLAPIStates.InSession || m_CurrentMLAPIState == StateToSceneTransitionLinks.MLAPIStates.Connecting)
            {

                //If we are in a session or connecting, then update the state first then switch scenes
                UpdateMLAPIState(newState);

                //Start the scene switch first
                SwitchScene(SceneName);

            }
            else
            {
                //Then update the MLAPI state
                UpdateMLAPIState(newState);

                //Start the scene switch first
                SwitchScene(SceneName);
            }

            m_GameState = newState;
        }
    }

    /// <summary>
    /// Sets the current game state
    /// </summary>
    /// <param name="sceneState"></param>
    public void SetGameState(GameStates gameState)
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening && m_GlobalGameStateComponent && IsServer)
        {
            m_GlobalGameStateComponent.SetNewGameState(gameState);
            GameStateChangedUpdate(m_GameState, gameState);
        }
        else
        {
            GameStateChangedUpdate(m_GameState, gameState);
        }
    }

    /// <summary>
    /// Returns the current scene state
    /// </summary>
    /// <returns>current scene state</returns>
    public GameStates GetCurrentSceneState()
    {
        return m_GameState;
    }

    /// <summary>
    /// Switches to a new scene
    /// </summary>
    /// <param name="scenename"></param>
    private void SwitchScene(string scenename)
    {
        // If we have started our network transport (connecting, connected, in game session, etc)
        if (NetworkManager.Singleton.IsListening && IsServer)
        {
            //Use the NetworkSceneManager
            m_SceneProgress = NetworkSceneManager.SwitchScene(scenename);

            //Use the SceneSwitchProgress OnClientLoadedScene event
            m_SceneProgress.OnClientLoadedScene += SceneProgress_OnClientLoadedScene;

            m_SceneProgress.OnComplete += OnLoadingComplete;
        }
        else
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                //If transport is not active then just load the scene via the normal Unity scene manager
                SceneManager.LoadScene(scenename);
            }
        }
    }

    /// <summary>
    /// When all clients have loaded their scene, this event is invoked
    /// </summary>
    /// <param name="timedOut">whether it timed out or not</param>
    private void OnLoadingComplete(bool timedOut)
    {
        //TODO: Handle message about timeOut
        if (m_SceneProgress != null && m_SceneProgress.IsAllClientsDoneLoading)
        {
            if (AllPlayersLoadedScene != null)
            {
                AllPlayersLoadedScene.Invoke();
            }
        }
    }

    /// <summary>
    /// Returns whether all clients are loaded
    /// </summary>
    /// <returns>true or false (they are all loaded or they are not)</returns>
    public bool AllClientsAreLoaded()
    {
        if (m_SceneProgress != null)
        {
            return m_SceneProgress.IsAllClientsDoneLoading;
        }
        return false;
    }

    /// <summary>
    /// Invoked when a client has finished loading a scene
    /// </summary>
    /// <param name="clientId">the client network id</param>
    private void SceneProgress_OnClientLoadedScene(ulong clientId)
    {
        if (ClientLoadedScene != null)
        {
            ClientLoadedScene.Invoke(clientId);
        }

    }

    #region IN-EDITOR SPECIFIC METHODS FOR AUTO-BOOTSTRAP LOADING
    /// <summary>
    /// Determined if we are loading from within the editor
    /// </summary>
    /// <returns>true or false</returns>
    public static bool IsLoadingFromEditor()
    {
#if (UNITY_EDITOR)
        return true;
#else
        return false;
#endif
    }

#if UNITY_EDITOR
    private static string s_BootStrapToScene;
    public static bool s_EditorLaunchingAsHost;
#endif

    /// <summary>
    /// Checks to see if we need to load the bootstrap for the current scene loaded in the editor
    /// </summary>
    /// <returns>true or false</returns>
    public static bool CheckForBootStrappedScene()
    {
#if UNITY_EDITOR
        if (s_BootStrapToScene != null && s_BootStrapToScene != string.Empty)
        {
            var BootStrappedGameState = Singleton.m_SceneToStateLinks.GetGameStateLinkedToScene(s_BootStrapToScene);
            s_BootStrapToScene = string.Empty;
            if (BootStrappedGameState != GameStates.None)
            {
                Singleton.IsHostingGame = s_EditorLaunchingAsHost;
                Singleton.UpdateMLAPIState(BootStrappedGameState);
                Singleton.SetGameState(BootStrappedGameState);
                return true;
            }
        }
#endif
        return false;
    }

    /// <summary>
    /// Loads the boot strap scene
    /// </summary>
    public static void LoadBootStrapScene()
    {
#if UNITY_EDITOR
        foreach (UnityEditor.EditorBuildSettingsScene nextscene in UnityEditor.EditorBuildSettings.scenes)
        {
            if (nextscene.enabled)
            {
                var SceneFilename = nextscene.path.Substring(nextscene.path.LastIndexOf('/')+1);
                var SplitSceneFileName = SceneFilename.Split('.');
                if (SplitSceneFileName[0] == "MLAPIBootStrap")
                {
                    var currentScene = SceneManager.GetActiveScene();
                    if (currentScene != null)
                    {
                        s_BootStrapToScene = currentScene.name;
                    }
                    SceneManager.LoadScene(SplitSceneFileName[0]);
                }
            }
        }
#endif
    }
    #endregion
}
